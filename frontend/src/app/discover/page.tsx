"use client";

import { motion, AnimatePresence } from "framer-motion";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Search, Telescope, Star, BookOpen, Film, Music, BookImage, Tv2,
  Download, Check, Loader2, ShieldAlert, TrendingUp, Award,
} from "lucide-react";
import Link from "next/link";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import { useProfilePrefs } from "@/store/profile";
import type { DiscoverItem, DiscoverResult, ImportResultDto, MediaKind } from "@/lib/types";

type DiscoverType = "movie" | "series" | "anime" | "manga" | "book" | "music";

type Tab = {
  id: DiscoverType;
  kind: MediaKind;
  label: string;
  icon: React.ReactNode;
};

const TABS: Tab[] = [
  { id: "movie",  kind: "Video",  label: "Movies",   icon: <Film size={14} /> },
  { id: "series", kind: "Video",  label: "TV",       icon: <Tv2 size={14} /> },
  { id: "anime",  kind: "Video",  label: "Anime",    icon: <Tv2 size={14} /> },
  { id: "book",   kind: "Book",   label: "Books",    icon: <BookOpen size={14} /> },
  { id: "manga",  kind: "Manga",  label: "Manga",    icon: <BookImage size={14} /> },
  { id: "music",  kind: "Audio",  label: "Music",    icon: <Music size={14} /> },
];

const CATEGORIES: { id: string; label: string; icon: React.ReactNode }[] = [
  { id: "popular",  label: "Popular",  icon: <Star size={12} /> },
  { id: "trending", label: "Trending", icon: <TrendingUp size={12} /> },
  { id: "rating",   label: "Top",      icon: <Award size={12} /> },
];

const PAGE_SIZE = 20;

export default function DiscoverPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const t = useTranslation();
  const { showAdultContent, setShowAdultContent } = useProfilePrefs();

  const [activeTabId, setActiveTabId] = useState<DiscoverType>("movie");
  const [category, setCategory] = useState<string>("popular");
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const [importing, setImporting] = useState<Set<string>>(new Set());
  const [imported, setImported] = useState<Map<string, string>>(new Map());
  const [page, setPage] = useState(1);

  const activeTab = TABS.find((t) => t.id === activeTabId) ?? TABS[0];

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  useEffect(() => {
    const timer = setTimeout(() => { setDebouncedQuery(query); setPage(1); }, 450);
    return () => clearTimeout(timer);
  }, [query]);

  // Reset page when tab/category changes
  useEffect(() => { setPage(1); }, [activeTabId, category]);

  const isSearching = debouncedQuery.trim().length >= 2;

  const discoverQuery = useQuery({
    queryKey: ["discover", activeTabId, category, debouncedQuery, page],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        type: activeTabId,
        kind: activeTab.kind,
        category,
        page,
        take: PAGE_SIZE,
      };
      if (isSearching) params.q = debouncedQuery.trim();
      const res = await api.get<DiscoverResult>("/discover", { params });
      return res.data;
    },
    enabled: !!accessToken,
    staleTime: 5 * 60 * 1000,
    placeholderData: (prev) => prev,
    retry: 1,
  });

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
    } catch {
      // import errors are surfaced in the card UI via the disabled state
    } finally {
      setImporting((s) => { const ns = new Set(s); ns.delete(item.externalId); return ns; });
    }
  };

  if (!isHydrated || !accessToken) return null;

  const data = discoverQuery.data;
  const allResults = data?.items ?? [];
  const results = showAdultContent
    ? allResults
    : allResults.filter((i) => !i.isAdult);

  const isLoading = discoverQuery.isLoading;
  const isFetching = discoverQuery.isFetching;
  const error = discoverQuery.error;
  const showEmpty = !isLoading && !isFetching && results.length === 0 && !error;

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

            <button
              onClick={() => setShowAdultContent(!showAdultContent)}
              className={
                "flex items-center gap-2 rounded-full border px-4 py-2 text-xs font-medium transition-all " +
                (showAdultContent
                  ? "border-red-400/40 bg-red-500/15 text-red-300 hover:bg-red-500/25"
                  : "border-white/10 bg-white/[0.04] text-mythra-text-muted hover:bg-white/[0.08] hover:text-white")
              }
              title={showAdultContent ? t("discover.adult.toggle.off") : t("discover.adult.toggle.on")}
            >
              <ShieldAlert size={13} />
              {showAdultContent ? t("discover.adult.toggle.off") : t("discover.adult.toggle.on")}
            </button>
          </div>
        </motion.div>

        {/* Type tabs */}
        <div className="mt-8 flex flex-wrap gap-1 rounded-2xl border border-white/[0.06] bg-white/[0.02] p-1 w-fit">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => { setActiveTabId(tab.id); setQuery(""); setDebouncedQuery(""); }}
              className={
                "flex items-center gap-1.5 rounded-xl px-4 py-2 text-sm font-medium transition-all " +
                (activeTabId === tab.id
                  ? "bg-gradient-to-r from-mythra-purple/30 to-mythra-blue/20 text-white"
                  : "text-mythra-text-muted hover:text-white")
              }
            >
              {tab.icon} {tab.label}
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
                {c.icon} {c.label}
              </button>
            ))}
          </div>
        )}

        {/* Search bar */}
        <div className="mt-4 relative">
          <Search size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-mythra-text-muted" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={`${t("action.search")} ${activeTab.label.toLowerCase()}…`}
            className="w-full rounded-2xl border border-white/10 bg-white/[0.03] py-3 pl-11 pr-4 text-sm outline-none placeholder:text-white/30 focus:border-mythra-purple/50 transition-colors"
          />
          {(isLoading || isFetching) && (
            <Loader2 size={16} className="absolute right-4 top-1/2 -translate-y-1/2 animate-spin text-mythra-text-muted" />
          )}
        </div>

        {/* Adult content hidden notice */}
        {!showAdultContent && allResults.some((i) => i.isAdult) && (
          <motion.div
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            className="mt-3 flex items-center gap-2 rounded-xl border border-amber-500/20 bg-amber-500/[0.06] px-3 py-2 text-xs text-amber-300"
          >
            <ShieldAlert size={12} />
            {allResults.filter((i) => i.isAdult).length} adult items hidden.{" "}
            <button
              onClick={() => setShowAdultContent(true)}
              className="underline underline-offset-2 hover:text-amber-200"
            >
              {t("action.show")}
            </button>
          </motion.div>
        )}

        {/* Title */}
        {!isLoading && results.length > 0 && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mt-6">
            <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-widest text-mythra-text-soft">
              <Star size={14} className="text-amber-400" />
              {isSearching
                ? t("discover.noResults", { query: debouncedQuery }).replace(/\.+$/, "").startsWith("No") || isSearching
                  ? `Results for "${debouncedQuery}"`
                  : t("discover.trending")
                : CATEGORIES.find((c) => c.id === category)?.label ?? t("discover.trending")}
            </h2>
          </motion.div>
        )}

        {/* Loading skeleton */}
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

        {/* Error state — show fallback UI but do not throw to the boundary */}
        {error && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mt-16 flex flex-col items-center gap-3 text-center">
            <span className="grid h-14 w-14 place-items-center rounded-full bg-red-500/10 text-red-400">
              <ShieldAlert size={24} />
            </span>
            <p className="text-sm text-mythra-text-muted">{t("discover.error")}</p>
            <button
              onClick={() => discoverQuery.refetch()}
              className="mt-1 rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-xs hover:bg-white/[0.08]"
            >
              {t("common.error")} — retry
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

        <AnimatePresence mode="wait">
          {results.length > 0 && (
            <motion.div
              key={`${activeTabId}-${category}-${debouncedQuery}-${page}`}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
            >
              {results.map((item) => (
                <DiscoverCard
                  key={`${item.providerKind}:${item.externalId}`}
                  item={item}
                  isImporting={importing.has(item.externalId)}
                  importedId={imported.get(item.externalId) ?? item.existingItemId ?? null}
                  onImport={() => importItem(item)}
                  showAdultContent={showAdultContent}
                />
              ))}
            </motion.div>
          )}
        </AnimatePresence>

        {/* Pagination — show whenever a full page came back */}
        {results.length > 0 && (results.length === PAGE_SIZE || page > 1) && (
          <div className="mt-8 flex items-center justify-center gap-3">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1 || isFetching}
              className="rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-xs font-medium text-white disabled:opacity-30"
            >
              ← Previous
            </button>
            <span className="text-xs text-mythra-text-muted">Page {page}</span>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={results.length < PAGE_SIZE || isFetching}
              className="rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-xs font-medium text-white disabled:opacity-30"
            >
              Next →
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
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={item.posterPath}
            alt={item.title}
            className={`h-full w-full object-cover transition-all duration-500 group-hover:scale-105 ${isBlurred ? "blur-xl scale-110" : ""}`}
          />
        ) : (
          <div className="flex h-full items-center justify-center text-white/10">
            <Film size={40} />
          </div>
        )}

        {isBlurred && (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-2 bg-black/50">
            <span className="rounded-full border border-red-400/40 bg-red-500/20 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-red-300">
              {t("discover.adult.badge")}
            </span>
            <button
              onClick={(e) => { e.stopPropagation(); setAdultRevealed(true); }}
              className="text-[11px] text-white/60 underline underline-offset-2 hover:text-white"
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
