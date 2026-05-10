"use client";

import { motion, AnimatePresence } from "framer-motion";
import { useQuery } from "@tanstack/react-query";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Search, Telescope, Star, BookOpen, Film, BookImage, Tv2,
  Download, Check, Loader2, ShieldAlert, TrendingUp, Award, Lock,
} from "lucide-react";
import Link from "next/link";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import { useProfilePrefs } from "@/store/profile";
import { SmartImage } from "@/components/ui/SmartImage";
import { useToasts } from "@/store/toasts";
import type { DiscoverItem, DiscoverResult, ImportResultDto, MediaKind } from "@/lib/types";
import type { TranslationKey } from "@/lib/i18n";

type DiscoverType = "movie" | "series" | "anime" | "manga" | "book";

type Tab = {
  id: DiscoverType;
  kind: MediaKind;
  labelKey: TranslationKey;
  icon: React.ReactNode;
};

const TABS: Tab[] = [
  { id: "movie",  kind: "Video",  labelKey: "discover.tab.movie",  icon: <Film size={14} /> },
  { id: "series", kind: "Video",  labelKey: "discover.tab.series", icon: <Tv2 size={14} /> },
  { id: "anime",  kind: "Video",  labelKey: "discover.tab.anime",  icon: <Tv2 size={14} /> },
  { id: "book",   kind: "Book",   labelKey: "discover.tab.book",   icon: <BookOpen size={14} /> },
  { id: "manga",  kind: "Manga",  labelKey: "discover.tab.manga",  icon: <BookImage size={14} /> },
];

const CATEGORIES: { id: string; labelKey: TranslationKey; icon: React.ReactNode }[] = [
  { id: "popular",  labelKey: "discover.category.popular",  icon: <Star size={12} /> },
  { id: "trending", labelKey: "discover.category.trending", icon: <TrendingUp size={12} /> },
  { id: "rating",   labelKey: "discover.category.top",      icon: <Award size={12} /> },
];

const PAGE_SIZE = 20;
const VALID_TAB_IDS = new Set<DiscoverType>(["movie", "series", "anime", "manga", "book"]);
const VALID_CATEGORIES = new Set(["popular", "trending", "rating"]);

function readHashState(): { tab: DiscoverType; cat: string; q: string; genre: string } {
  if (typeof window === "undefined") {
    return { tab: "movie", cat: "popular", q: "", genre: "" };
  }
  const hash = window.location.hash.startsWith("#")
    ? window.location.hash.slice(1)
    : window.location.hash;
  const params = new URLSearchParams(hash);
  const tabRaw = params.get("tab") ?? "movie";
  const tab = (VALID_TAB_IDS.has(tabRaw as DiscoverType) ? tabRaw : "movie") as DiscoverType;
  const catRaw = params.get("cat") ?? "popular";
  const cat = VALID_CATEGORIES.has(catRaw) ? catRaw : "popular";
  const q = params.get("q") ?? "";
  const genre = params.get("genre") ?? "";
  return { tab, cat, q, genre };
}

function buildHash(state: { tab: DiscoverType; cat: string; q: string; genre: string }): string {
  const params = new URLSearchParams();
  params.set("tab", state.tab);
  params.set("cat", state.cat);
  if (state.q) params.set("q", state.q);
  if (state.genre) params.set("genre", state.genre);
  return params.toString();
}

export default function DiscoverPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const t = useTranslation();
  const { showAdultContent } = useProfilePrefs();

  const initial = typeof window !== "undefined" ? readHashState() : { tab: "movie" as DiscoverType, cat: "popular", q: "", genre: "" };

  const [activeTabId, setActiveTabId] = useState<DiscoverType>(initial.tab);
  const [category, setCategory] = useState<string>(initial.cat);
  const [query, setQuery] = useState(initial.q);
  const [debouncedQuery, setDebouncedQuery] = useState(initial.q);
  const [genre, setGenre] = useState(initial.genre);
  const [importing, setImporting] = useState<Set<string>>(new Set());
  const [imported, setImported] = useState<Map<string, string>>(new Map());

  // Accumulated load-more state
  const [currentPage, setCurrentPage] = useState(1);
  const [allItems, setAllItems] = useState<DiscoverItem[]>([]);
  const [hasMore, setHasMore] = useState(true);

  const activeTab = TABS.find((tab) => tab.id === activeTabId) ?? TABS[0];

  // Sync state to URL hash (no page in hash)
  const skipNextHashSync = useRef(false);
  useEffect(() => {
    if (typeof window === "undefined") return;
    if (skipNextHashSync.current) {
      skipNextHashSync.current = false;
      return;
    }
    const next = buildHash({ tab: activeTabId, cat: category, q: debouncedQuery, genre });
    const target = `#${next}`;
    if (window.location.hash !== target) {
      window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}${target}`);
    }
  }, [activeTabId, category, debouncedQuery]);

  // Listen to hashchange for back/forward
  useEffect(() => {
    if (typeof window === "undefined") return;
    const onHashChange = () => {
      const s = readHashState();
      skipNextHashSync.current = true;
      setActiveTabId(s.tab);
      setCategory(s.cat);
      setQuery(s.q);
      setDebouncedQuery(s.q);
      setGenre(s.genre);
    };
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQuery(query);
    }, 300);
    return () => clearTimeout(timer);
  }, [query]);

  // Reset accumulated items whenever filter parameters change
  const filterFingerprint = `${activeTabId}|${category}|${debouncedQuery}|${genre}`;
  const lastFingerprintRef = useRef(filterFingerprint);
  useEffect(() => {
    if (lastFingerprintRef.current === filterFingerprint) return;
    lastFingerprintRef.current = filterFingerprint;
    setCurrentPage(1);
    setAllItems([]);
    setHasMore(true);
  }, [filterFingerprint]);

  const isSearching = debouncedQuery.trim().length >= 2;

  const pageQuery = useQuery({
    queryKey: ["discover", activeTabId, category, debouncedQuery, genre, currentPage],
    queryFn: async () => {
      const params: Record<string, string | number | boolean> = {
        type: activeTabId,
        kind: activeTab.kind,
        category,
        page: currentPage,
        take: PAGE_SIZE,
        includeAdult: true,
      };
      if (isSearching) params.q = debouncedQuery.trim();
      if (genre.trim()) params.genre = genre.trim();
      const res = await api.get<DiscoverResult>("/discover", { params });
      return res.data;
    },
    enabled: !!accessToken,
    staleTime: 30_000,
    retry: 2,
    refetchOnMount: "always",
    refetchOnWindowFocus: false,
  });

  // Accumulate results from each page
  useEffect(() => {
    if (!pageQuery.isSuccess || !pageQuery.data) return;
    const items = pageQuery.data.items ?? [];
    // Only accumulate if the fingerprint still matches (prevents stale appends)
    if (lastFingerprintRef.current !== filterFingerprint) return;
    if (currentPage === 1) {
      setAllItems(items);
    } else {
      setAllItems((prev) => {
        const seen = new Set(prev.map((i) => i.externalId));
        return [...prev, ...items.filter((i) => !seen.has(i.externalId))];
      });
    }
    setHasMore(items.length >= PAGE_SIZE);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageQuery.data, pageQuery.isSuccess]);

  const qc = useQueryClient();
  const pushToast = useToasts((s) => s.push);

  const importItem = async (item: DiscoverItem) => {
    setImporting((s) => new Set(s).add(item.externalId));
    try {
      const res = await api.post<ImportResultDto>("/discover/import", {
        externalId: item.externalId,
        providerKind: item.providerKind,
        mediaKind: activeTab.kind,
        targetLibraryId: null,
      });
      setImported((m) => new Map(m).set(item.externalId, res.data.id));
      qc.invalidateQueries({ queryKey: ["library"] });
      qc.invalidateQueries({ queryKey: ["recent"] });
      qc.invalidateQueries({ queryKey: ["videos"] });
      qc.invalidateQueries({ queryKey: ["mangas"] });
      qc.invalidateQueries({ queryKey: ["books"] });
      pushToast({
        kind: "success",
        message: t("discover.import.success", { title: res.data.title ?? item.title }),
        action: {
          label: t("discover.import.successAction"),
          onClick: () => router.push(`/item/${res.data.id}`),
        },
      });
      router.push(`/item/${res.data.id}`);
    } catch (e) {
      const detail =
        (e as { response?: { data?: { detail?: string; title?: string } } })
          ?.response?.data;
      const message = detail?.detail ?? detail?.title;
      pushToast({
        kind: "error",
        message: message
          ? t("discover.import.error", { message })
          : t("discover.import.errorFallback"),
        duration: 8000,
        action: {
          label: t("discover.import.retry"),
          onClick: () => importItem(item),
        },
      });
    } finally {
      setImporting((s) => { const ns = new Set(s); ns.delete(item.externalId); return ns; });
    }
  };

  if (!isHydrated || !accessToken) return null;

  const isLoading = pageQuery.isLoading && currentPage === 1;
  const isFetchingMore = pageQuery.isFetching && currentPage > 1;
  const error = pageQuery.error && allItems.length === 0;
  const showEmpty = !pageQuery.isLoading && !pageQuery.isFetching && allItems.length === 0 && !pageQuery.error;

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <div className="flex items-start justify-between gap-4 flex-wrap">
            <div className="flex items-center gap-3">
              <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-mythra-purple to-mythra-blue text-white">
                <Telescope size={20} />
              </span>
              <div>
                <h1 className="text-3xl font-bold tracking-tight md:text-4xl">
                  <span className="gradient-text">{t("discover.title")}</span>
                </h1>
                <p className="text-sm text-mythra-text-muted">{t("discover.subtitle")}</p>
              </div>
            </div>
          </div>
        </motion.div>

        {/* Type tabs */}
        <div className="mt-8 flex flex-wrap gap-1 rounded-2xl border border-white/[0.06] bg-white/[0.02] p-1 w-fit">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => {
                setActiveTabId(tab.id);
                setQuery("");
                setDebouncedQuery("");
                setGenre("");
              }}
              className={
                "flex items-center gap-1.5 rounded-xl px-4 py-2 text-sm font-medium transition-all " +
                (activeTabId === tab.id
                  ? "bg-gradient-to-r from-mythra-purple/30 to-mythra-blue/20 text-white"
                  : "text-mythra-text-muted hover:text-white")
              }
            >
              {tab.icon} {t(tab.labelKey)}
            </button>
          ))}
        </div>

        {/* Category pills (catalog browsing) */}
        {!isSearching && (
          <div className="mt-3 flex flex-wrap gap-1.5">
            {CATEGORIES.map((c) => (
              <button
                key={c.id}
                onClick={() => setCategory(c.id)}
                className={
                  "flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-medium transition-all " +
                  (category === c.id
                    ? "border-mythra-purple/40 bg-mythra-purple/15 text-white"
                    : "border-white/10 bg-white/[0.02] text-mythra-text-muted hover:text-white")
                }
              >
                {c.icon} {t(c.labelKey)}
              </button>
            ))}
          </div>
        )}

        {/* Search bar */}
        <div className="mt-4 flex gap-2">
          <div className="relative flex-1">
            <Search size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-mythra-text-muted" />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={`${t("action.search")} ${t(activeTab.labelKey).toLowerCase()}…`}
              className="w-full rounded-2xl border border-white/10 bg-white/[0.03] py-3 pl-11 pr-4 text-sm outline-none placeholder:text-white/30 focus:border-mythra-purple/50 transition-colors"
            />
            {pageQuery.isFetching && (
              <Loader2 size={16} className="absolute right-4 top-1/2 -translate-y-1/2 animate-spin text-mythra-text-muted" />
            )}
          </div>
          <div className="relative">
            <input
              value={genre}
              onChange={(e) => setGenre(e.target.value)}
              placeholder={t("discover.genrePlaceholder")}
              className="w-36 rounded-2xl border border-white/10 bg-white/[0.03] py-3 px-4 text-sm outline-none placeholder:text-white/30 focus:border-mythra-blue/50 transition-colors"
            />
            {genre && (
              <button
                onClick={() => setGenre("")}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-mythra-text-muted hover:text-white"
              >
                <span className="text-xs">✕</span>
              </button>
            )}
          </div>
        </div>

        {/* Title */}
        {!isLoading && allItems.length > 0 && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mt-6">
            <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-widest text-mythra-text-soft">
              <Star size={14} className="text-amber-400" />
              {isSearching
                ? t("discover.results.for", { query: debouncedQuery })
                : (() => {
                    const cat = CATEGORIES.find((c) => c.id === category);
                    return cat ? t(cat.labelKey) : t("discover.trending");
                  })()}
            </h2>
          </motion.div>
        )}

        {/* Loading skeleton (first page only) */}
        {isLoading && (
          <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="animate-pulse rounded-2xl border border-white/[0.06] bg-white/[0.02]">
                <div className="aspect-[2/3] rounded-t-2xl bg-white/[0.04]" />
                <div className="p-3 space-y-2">
                  <div className="h-4 w-3/4 rounded bg-white/[0.06]" />
                  <div className="h-3 w-1/2 rounded bg-white/[0.04]" />
                  <div className="h-8 w-full rounded-xl bg-white/[0.04] mt-3" />
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Error state */}
        {error && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mt-16 flex flex-col items-center gap-3 text-center">
            <span className="grid h-14 w-14 place-items-center rounded-full bg-red-500/10 text-red-400">
              <ShieldAlert size={24} />
            </span>
            <p className="text-sm text-mythra-text-muted">{t("discover.error")}</p>
            <button
              onClick={() => pageQuery.refetch()}
              className="mt-1 rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-xs hover:bg-white/[0.08]"
            >
              {t("discover.retry")}
            </button>
          </motion.div>
        )}

        {/* Empty state */}
        {showEmpty && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="mt-24 flex flex-col items-center gap-4 text-center"
          >
            <span className="grid h-20 w-20 place-items-center rounded-full bg-white/[0.03]">
              <Telescope size={36} className="text-mythra-text-muted" />
            </span>
            <p className="text-mythra-text-muted">
              {isSearching ? t("discover.noResults", { query: debouncedQuery }) : t("discover.empty")}
            </p>
          </motion.div>
        )}

        {/* Results grid */}
        <AnimatePresence mode="popLayout">
          {allItems.length > 0 && (
            <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {allItems.map((item, i) => (
                <motion.div
                  key={`${item.providerKind}:${item.externalId}`}
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.25, delay: Math.min(i % PAGE_SIZE, 8) * 0.04 }}
                  layout
                >
                  <DiscoverCard
                    item={item}
                    isImporting={importing.has(item.externalId)}
                    importedId={imported.get(item.externalId) ?? item.existingItemId ?? null}
                    onImport={() => importItem(item)}
                    showAdultContent={showAdultContent}
                  />
                </motion.div>
              ))}
            </div>
          )}
        </AnimatePresence>

        {/* Load more button */}
        {allItems.length > 0 && hasMore && (
          <div className="mt-10 flex justify-center">
            <button
              onClick={() => setCurrentPage((p) => p + 1)}
              disabled={pageQuery.isFetching}
              className="flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-8 py-3 text-sm font-medium text-white transition hover:bg-white/[0.08] disabled:opacity-50"
            >
              {isFetchingMore ? (
                <><Loader2 size={14} className="animate-spin" /> {t("action.loading")}</>
              ) : (
                t("discover.loadMore")
              )}
            </button>
          </div>
        )}
      </PageScaffold>
    </>
  );
}

// ── Discover card ────────────────────────────────────────────────────────────

function DiscoverCard({
  item, isImporting, importedId, onImport, showAdultContent,
}: {
  item: DiscoverItem;
  isImporting: boolean;
  importedId: string | null;
  onImport: () => void;
  showAdultContent: boolean;
}) {
  const t = useTranslation();
  const alreadyImported = !!(importedId ?? item.alreadyImported);
  const [adultRevealed, setAdultRevealed] = useState(false);
  const isBlurred = item.isAdult && !showAdultContent && !adultRevealed;

  return (
    <motion.div
      layout
      whileHover={{ y: -2 }}
      className="group relative overflow-hidden rounded-2xl border border-white/[0.06] bg-white/[0.02] transition-colors hover:border-white/10"
    >
      <div className="relative aspect-[2/3] overflow-hidden bg-white/[0.04]">
        {item.posterPath ? (
          <SmartImage
            src={item.posterPath}
            alt={item.title}
            className={`h-full w-full object-cover transition-all duration-500 group-hover:scale-105 ${isBlurred ? "blur-xl scale-110" : ""}`}
            style={isBlurred ? { filter: "blur(24px) brightness(0.6)" } : undefined}
            fallbackKind="poster"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-white/10">
            <Film size={40} />
          </div>
        )}

        {isBlurred && (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-2 bg-black/50">
            <Lock size={20} className="text-white/70" />
            <span className="rounded-full border border-red-400/40 bg-red-500/20 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-red-300">
              {t("discover.adult.badge")}
            </span>
            <button
              onClick={(e) => { e.stopPropagation(); e.preventDefault(); setAdultRevealed(true); }}
              className="rounded-full border border-white/20 bg-white/10 px-3 py-1 text-[11px] font-medium text-white hover:bg-white/20"
            >
              {t("discover.adult.show")}
            </button>
          </div>
        )}

        {item.isAdult && (showAdultContent || adultRevealed) && (
          <span className="absolute left-2 top-2 rounded-full border border-red-400/30 bg-red-500/20 px-1.5 py-0.5 text-[9px] font-bold text-red-300">
            +18
          </span>
        )}
      </div>

      {item.rating != null && !isBlurred && (
        <div className="absolute right-2 top-2 flex items-center gap-1 rounded-full bg-black/70 px-2 py-0.5 text-xs backdrop-blur">
          <Star size={10} className="text-amber-400" />
          <span>{item.rating.toFixed(1)}</span>
        </div>
      )}

      <div className="p-3">
        <p className="line-clamp-1 text-sm font-medium text-white">{item.title}</p>
        {item.originalTitle && item.originalTitle !== item.title && (
          <p className="line-clamp-1 text-[11px] text-mythra-text-muted">{item.originalTitle}</p>
        )}
        <p className="mt-0.5 text-[11px] text-mythra-text-muted">
          {item.year ?? "—"}
          {item.genres.length > 0 && ` · ${item.genres.slice(0, 2).join(", ")}`}
        </p>

        <div className="mt-3 flex gap-2">
          {alreadyImported ? (
            <Link
              href={`/item/${importedId ?? item.existingItemId}`}
              className="flex flex-1 items-center justify-center gap-1.5 rounded-xl border border-emerald-500/30 bg-emerald-500/10 py-2 text-xs text-emerald-300"
            >
              <Check size={12} /> {t("action.inLibrary")}
            </Link>
          ) : (
            <button
              onClick={onImport}
              disabled={isImporting}
              className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-gradient-to-r from-mythra-purple to-mythra-blue py-2 text-xs font-medium text-white disabled:opacity-60"
            >
              {isImporting ? (
                <><Loader2 size={12} className="animate-spin" /> {t("action.importing")}</>
              ) : (
                <><Download size={12} /> {t("action.import")}</>
              )}
            </button>
          )}
        </div>
      </div>
    </motion.div>
  );
}
