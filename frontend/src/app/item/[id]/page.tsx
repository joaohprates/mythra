"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { motion, AnimatePresence } from "framer-motion";
import { Bookmark, ChevronDown, Download, Library, Play, Trash2, AlertTriangle } from "lucide-react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Topbar } from "@/components/shell/Topbar";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { cn } from "@/lib/cn";
import { cleanDescription } from "@/lib/text";
import { useTranslation } from "@/store/locale";
import { SmartImage } from "@/components/ui/SmartImage";
import { useHasDownloadAddon, useHasStreamAddon } from "@/hooks/useAddonCapabilities";
import { DownloadDialog } from "@/components/media/DownloadDialog";

interface ItemAny {
  id: string;
  kind: "Video" | "Manga" | "Book";
  title: string;
  overview?: string | null;
  tagline?: string | null;
  posterPath?: string | null;
  backdropPath?: string | null;
  year?: number | null;
  rating?: number | null;
  genres?: string[];
  duration?: string | null;
  resolutionLabel?: string;
  author?: string | null;
  artist?: string | null;
  publisher?: string | null;
  pageCount?: number | null;
  totalChapters?: number | null;
  videoKind?: string;
  isAnime?: boolean;
  chapters?: { id: string; title: string }[];
  fileStatus?: string;
  hasFile?: boolean;
  imdbId?: string | null;
  parentId?: string | null;
  language?: string | null;
}

/** Resolve the capability map key for a given item. */
function resolveCapabilityKey(item: Pick<ItemAny, "kind" | "videoKind" | "isAnime">): string {
  if (item.kind === "Manga") return "manga";
  if (item.kind === "Book") return "book";
  if (item.kind === "Video") {
    if (item.isAnime) return "anime";
    const vk = (item.videoKind ?? "").toLowerCase();
    if (vk === "series" || vk === "season" || vk === "episode") return "series";
    if (vk === "anime" || vk === "animemovie") return "anime";
    return "movie";
  }
  return "video";
}

interface EpisodeDto {
  id: string;
  title: string;
  overview?: string | null;
  posterPath?: string | null;
  seasonNumber?: number | null;
  episodeNumber?: number | null;
  duration?: string | null;
  year?: number | null;
  hasFile: boolean;
  imdbId?: string | null;
}

const SERIES_KINDS = new Set(["Series", "Anime"]);

export default function ItemDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const t = useTranslation();

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const detail = useQuery({
    queryKey: ["item-detail", params.id],
    queryFn: async () => (await api.get<ItemAny>(`/items/${params.id}`)).data,
    enabled: !!params.id && !!accessToken,
  });

  const item = detail.data;
  const isSeries = item?.kind === "Video" && SERIES_KINDS.has(item?.videoKind ?? "");

  const episodes = useQuery({
    queryKey: ["episodes", params.id],
    queryFn: async () => (await api.get<EpisodeDto[]>(`/items/${params.id}/episodes`)).data,
    enabled: isSeries,
  });

  return (
    <>
      <Topbar />
      <main className="relative min-h-[60vh]">
        {item?.backdropPath && (
          <motion.div
            initial={{ opacity: 0, scale: 1.04 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 1.1, ease: [0.16, 1, 0.3, 1] }}
            className="absolute inset-x-0 top-0 -z-10 h-[70vh] overflow-hidden"
          >
            <SmartImage src={item.backdropPath} alt="" className="h-full w-full object-cover" fallbackKind="backdrop" loading="eager" />
            <div className="absolute inset-0 bg-gradient-to-b from-black/40 via-black/65 to-mythra-bg" />
          </motion.div>
        )}

        <section className="mx-auto max-w-[1500px] px-6 pb-24 pt-32 lg:px-10">
          {item ? (
            <motion.div
              initial={{ opacity: 0, y: 32, filter: "blur(8px)" }}
              animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
              transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1] }}
            >
              {/* ── Hero row ── */}
              <div className="grid gap-10 md:grid-cols-[280px_1fr]">
                <div className="relative aspect-[2/3] overflow-hidden rounded-3xl border border-white/[0.06] shadow-mythra-card">
                  {item.posterPath ? (
                    <SmartImage src={item.posterPath} alt={item.title} className="h-full w-full object-cover" fallbackKind="poster" />
                  ) : (
                    <div className="grid h-full w-full place-items-center bg-gradient-to-br from-[#1a1d35] to-[#070811] text-mythra-text-soft">
                      {item.kind}
                    </div>
                  )}
                </div>

                <div>
                  <div className="flex items-center gap-2">
                    <span className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-[11px] uppercase tracking-widest text-mythra-text-soft">
                      {item.kind}
                      {item.videoKind && ` • ${item.videoKind}`}
                    </span>
                    {item.genres?.some(g => ["Hentai", "Ecchi", "Adult", "Adult Content", "Pornography", "Eroge"].includes(g)) && (
                      <span className="inline-flex items-center gap-1 rounded-full border border-red-400/30 bg-red-500/20 px-2 py-0.5 text-[10px] font-bold text-red-300">
                        +18
                      </span>
                    )}
                  </div>
                  <h1 className="mt-4 text-4xl font-bold tracking-tight md:text-6xl">
                    <span className="gradient-text">{item.title}</span>
                  </h1>

                  <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-mythra-text-soft">
                    {item.year && <span>{item.year}</span>}
                    {item.author && <span>{t("item.byAuthor", { author: item.author })}</span>}
                    {item.publisher && <span>{item.publisher}</span>}
                    {item.duration && <span>{formatDuration(item.duration)}</span>}
                    {item.resolutionLabel && (
                      <span className="rounded-full border border-white/10 px-2 py-0.5">{item.resolutionLabel}</span>
                    )}
                    {item.pageCount && <span>{t("item.pages", { count: String(item.pageCount) })}</span>}
                    {item.totalChapters && <span>{t("item.chaptersCount", { count: String(item.totalChapters) })}</span>}
                    {item.rating && (
                      <span className="rounded-full bg-amber-300/15 px-2 py-0.5 text-amber-200">
                        ★ {item.rating.toFixed(1)}
                      </span>
                    )}
                    {item.imdbId && (
                      <a
                        href={`https://www.imdb.com/title/${item.imdbId}/`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="rounded-full border border-amber-400/30 bg-amber-400/10 px-2 py-0.5 text-amber-300 hover:bg-amber-400/20"
                      >
                        IMDB ↗
                      </a>
                    )}
                  </div>

                  {item.overview && (
                    <p className="mt-5 max-w-3xl text-sm leading-relaxed text-mythra-text-muted md:text-base whitespace-pre-line">
                      {cleanDescription(item.overview)}
                    </p>
                  )}

                  <div className="mt-7 flex flex-wrap gap-3">
                    {!isSeries && <PrimaryAction item={item} />}
                    <ActionBtn label={t("item.action.playlist")} icon={<Bookmark size={16} />} />
                    <Link
                      href={`/library/all/${item.kind}`}
                      className="inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.04] px-5 py-3 text-sm font-medium text-white backdrop-blur transition hover:bg-white/[0.08]"
                    >
                      <Library size={16} /> {t("nav.library")}
                    </Link>
                    <DownloadAction item={item} />
                    <RemoveFromLibraryBtn id={item.id} title={item.title} />
                  </div>

                  {item.genres && item.genres.length > 0 && (
                    <div className="mt-6 flex flex-wrap gap-2">
                      {item.genres.map((g) => (
                        <span key={g} className="rounded-full border border-white/10 px-3 py-1 text-xs text-mythra-text-muted">
                          {g}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {/* ── Series episodes (Netflix-style) ── */}
              {isSeries && (
                <div className="mt-16">
                  <SeriesView episodes={episodes.data ?? []} loading={episodes.isLoading} />
                </div>
              )}

              {/* ── Book / manga chapters (non-series) ── */}
              {!isSeries && item.chapters && item.chapters.length > 0 && (
                <div className="mt-10">
                  <h3 className="mb-3 text-sm font-semibold uppercase tracking-widest text-mythra-text-soft">{t("item.chapters")}</h3>
                  <ul className="grid gap-2 md:grid-cols-2">
                    {item.chapters.slice(0, 12).map((c, i) => (
                      <li
                        key={c.id}
                        className="flex items-center gap-3 rounded-xl border border-white/[0.05] bg-white/[0.02] p-3"
                      >
                        <span className="grid h-7 w-7 place-items-center rounded-full bg-white/10 text-[11px]">{i + 1}</span>
                        <span className="text-sm text-white">{c.title}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </motion.div>
          ) : (
            <div className="flex h-[60vh] items-center justify-center text-mythra-text-soft">{t("item.loading")}</div>
          )}
        </section>
      </main>
    </>
  );
}

// ── Series View ──────────────────────────────────────────────────────────────

function SeriesView({ episodes, loading }: { episodes: EpisodeDto[]; loading: boolean }) {
  const t = useTranslation();
  const seasons = groupBySeason(episodes);
  const seasonNumbers = Object.keys(seasons)
    .map(Number)
    .sort((a, b) => a - b);
  const [activeSeason, setActiveSeason] = useState<number | null>(null);
  const currentSeason = activeSeason ?? seasonNumbers[0] ?? 0;

  if (loading) {
    return <div className="text-center text-mythra-text-muted text-sm py-12">{t("item.episodesLoading")}</div>;
  }

  if (episodes.length === 0) {
    return <div className="text-center text-mythra-text-muted text-sm py-12">{t("item.noEpisodes")}</div>;
  }

  return (
    <div>
      <div className="mb-6 flex items-center gap-4">
        <h2 className="text-xl font-bold">{t("item.episodes")}</h2>
        {/* Season picker */}
        {seasonNumbers.length > 1 && (
          <SeasonSelector
            seasons={seasonNumbers}
            active={currentSeason}
            onChange={setActiveSeason}
          />
        )}
      </div>

      <div className="space-y-3">
        <AnimatePresence mode="wait">
          <motion.div
            key={currentSeason}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.25 }}
          >
            {(seasons[currentSeason] ?? []).map((ep) => (
              <EpisodeRow key={ep.id} ep={ep} />
            ))}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
}

function SeasonSelector({
  seasons,
  active,
  onChange,
}: {
  seasons: number[];
  active: number;
  onChange: (s: number) => void;
}) {
  const [open, setOpen] = useState(false);
  const t = useTranslation();
  const seasonLabel = (n: number) => (n === 0 ? t("item.specials") : t("item.season", { n: String(n) }));
  return (
    <div className="relative">
      <button
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium transition hover:bg-white/[0.08]"
      >
        {seasonLabel(active)}
        <ChevronDown size={14} className={cn("transition-transform", open && "rotate-180")} />
      </button>
      <AnimatePresence>
        {open && (
          <motion.ul
            initial={{ opacity: 0, y: 6, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 6, scale: 0.97 }}
            transition={{ duration: 0.15 }}
            className="absolute left-0 top-full z-20 mt-2 min-w-[140px] overflow-hidden rounded-2xl border border-white/10 bg-[#0c0e1a] shadow-2xl"
          >
            {seasons.map((s) => (
              <li key={s}>
                <button
                  onClick={() => { onChange(s); setOpen(false); }}
                  className={cn(
                    "w-full px-4 py-2.5 text-left text-sm transition hover:bg-white/[0.06]",
                    s === active && "text-mythra-purple"
                  )}
                >
                  {seasonLabel(s)}
                </button>
              </li>
            ))}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  );
}

function EpisodeRow({ ep }: { ep: EpisodeDto }) {
  const t = useTranslation();
  const label = ep.episodeNumber != null ? `E${ep.episodeNumber}` : "";

  return (
    <motion.div
      initial={{ opacity: 0, x: -8 }}
      animate={{ opacity: 1, x: 0 }}
      className="group flex items-start gap-4 rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4 transition hover:bg-white/[0.05]"
    >
      {/* Thumbnail / episode number */}
      <div className="relative h-[72px] w-[128px] shrink-0 overflow-hidden rounded-xl bg-gradient-to-br from-[#1a1d35] to-[#070811]">
        {ep.posterPath ? (
          <SmartImage src={ep.posterPath} alt={ep.title} className="h-full w-full object-cover" fallbackKind="backdrop" />
        ) : (
          <div className="grid h-full w-full place-items-center text-mythra-text-soft text-xs">{label}</div>
        )}
        {/* Play overlay */}
        <Link
          href={`/watch/${ep.id}`}
          className="absolute inset-0 flex items-center justify-center bg-black/50 opacity-0 transition-opacity group-hover:opacity-100"
        >
          <span className="grid h-9 w-9 place-items-center rounded-full bg-white text-black shadow">
            <Play size={14} className="fill-current" />
          </span>
        </Link>
      </div>

      {/* Info */}
      <div className="flex min-w-0 flex-1 flex-col">
        <div className="flex items-center gap-2">
          {label && (
            <span className="shrink-0 rounded-full border border-white/10 px-2 py-0.5 text-[11px] text-mythra-text-soft">
              {label}
            </span>
          )}
          <span className="truncate text-sm font-semibold">{ep.title}</span>
          {ep.duration && (
            <span className="ml-auto shrink-0 text-xs text-mythra-text-soft">{formatDuration(ep.duration)}</span>
          )}
        </div>
        {ep.overview && (
          <p className="mt-1.5 line-clamp-2 text-xs leading-relaxed text-mythra-text-muted whitespace-pre-line">
            {cleanDescription(ep.overview)}
          </p>
        )}
        {/* Streaming badge */}
        {!ep.hasFile && (
          <span className="mt-2 self-start rounded-full border border-mythra-purple/30 bg-mythra-purple/10 px-2 py-0.5 text-[10px] text-mythra-purple">
            {t("item.external")}
          </span>
        )}
      </div>
    </motion.div>
  );
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function groupBySeason(episodes: EpisodeDto[]): Record<number, EpisodeDto[]> {
  const map: Record<number, EpisodeDto[]> = {};
  for (const ep of episodes) {
    const s = ep.seasonNumber ?? 0;
    if (!map[s]) map[s] = [];
    map[s].push(ep);
  }
  // Sort episodes within each season
  for (const s of Object.keys(map)) {
    map[Number(s)].sort((a, b) => (a.episodeNumber ?? 0) - (b.episodeNumber ?? 0));
  }
  return map;
}

function ActionBtn({ label, icon, onClick }: { label: string; icon: React.ReactNode; onClick?: () => void }) {
  return (
    <button
      onClick={onClick}
      className="inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.04] px-5 py-3 text-sm font-medium text-white backdrop-blur transition hover:bg-white/[0.08]"
    >
      {icon} {label}
    </button>
  );
}

function PrimaryAction({ item }: { item: ItemAny }) {
  const t = useTranslation();
  const capabilityKey = resolveCapabilityKey(item);
  const hasStreamAddon = useHasStreamAddon(capabilityKey);
  const hasFile = item.hasFile !== false;

  // Spec: when no local file AND no streaming addon for this kind → render nothing
  // (no Play button, no hint banner).
  if (!hasFile && !hasStreamAddon) {
    return null;
  }

  // External provider exists but stream resolution may still fail — keep the
  // hint banner ONLY when the item is external-only and an addon should resolve it.
  const externalNeedsAddon =
    !hasFile && item.fileStatus === "ExternalOnly" && !item.imdbId && hasStreamAddon;
  if (externalNeedsAddon) {
    return (
      <div className="flex flex-col gap-2">
        <Link
          href={item.kind === "Video" ? `/watch/${item.id}` : `/read/${item.id}`}
          className="inline-flex items-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold text-black shadow-[0_18px_50px_-15px_rgba(255,255,255,0.6)] transition hover:scale-[1.03]"
        >
          {item.kind === "Video"
            ? <><Play size={16} className="fill-current" /> {t("action.watchNow")}</>
            : <><Library size={16} /> {t("action.readNow")}</>}
        </Link>
        <span className="inline-flex items-center gap-1.5 text-[11px] text-amber-300">
          <AlertTriangle size={12} /> {t("item.external")}
        </span>
      </div>
    );
  }

  const cfg = {
    Video: { href: `/watch/${item.id}`, icon: <Play size={16} className="fill-current" />, label: t("action.watchNow") },
    Manga: { href: `/read/${item.id}`, icon: <Library size={16} />, label: t("action.readNow") },
    Book: { href: `/read/${item.id}`, icon: <Library size={16} />, label: t("action.readNow") },
  } as const;
  const c = cfg[item.kind as keyof typeof cfg] ?? cfg.Video;
  return (
    <Link
      href={c.href}
      className="inline-flex items-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold text-black shadow-[0_18px_50px_-15px_rgba(255,255,255,0.6)] transition hover:scale-[1.03]"
    >
      {c.icon} {c.label}
    </Link>
  );
}

function DownloadAction({ item }: { item: ItemAny }) {
  const t = useTranslation();
  const capabilityKey = resolveCapabilityKey(item);
  const hasDownloadAddon = useHasDownloadAddon(capabilityKey);
  const hasLocalFile = item.fileStatus === "Available" && item.hasFile !== false;
  const [open, setOpen] = useState(false);

  // Show button when an addon offers downloads OR when there's a local file.
  if (!hasDownloadAddon && !hasLocalFile) return null;

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.04] px-5 py-3 text-sm font-medium text-white backdrop-blur transition hover:bg-white/[0.08]"
      >
        <Download size={16} /> {t("action.download")}
      </button>
      <DownloadDialog
        itemId={item.id}
        open={open}
        onClose={() => setOpen(false)}
        fallback={{ language: item.language ?? null }}
        noAddon={!hasDownloadAddon && !hasLocalFile}
      />
    </>
  );
}

function RemoveFromLibraryBtn({ id, title }: { id: string; title: string }) {
  const t = useTranslation();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRemove = async () => {
    if (!confirm(t("item.removeConfirm", { title }))) return;
    setDeleting(true);
    setError(null);
    try {
      await api.delete(`/items/${id}`);
      // Drop the cached detail entirely and invalidate everything that could
      // reference the item — home rows, library grids, favorites, playlists,
      // recommendations, search results, recently-added.
      queryClient.removeQueries({ queryKey: ["item-detail", id] });
      await queryClient.invalidateQueries();
      router.replace("/");
    } catch (err: unknown) {
      const detail =
        (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data;
      setError(detail?.detail ?? detail?.title ?? t("item.removeFailed"));
      setDeleting(false);
    }
  };

  return (
    <div className="flex flex-col gap-1">
      <button
        onClick={handleRemove}
        disabled={deleting}
        className="inline-flex items-center gap-2 rounded-full border border-red-400/20 bg-red-500/10 px-5 py-3 text-sm font-medium text-red-300 backdrop-blur transition hover:bg-red-500/20 disabled:opacity-50"
      >
        <Trash2 size={16} /> {deleting ? "…" : t("action.removeFromLibrary")}
      </button>
      {error && (
        <span className="text-[11px] text-red-300/80">{error}</span>
      )}
    </div>
  );
}

function formatDuration(ts: string): string {
  const parts = ts.split(":");
  if (parts.length !== 3) return ts;
  const h = parseInt(parts[0], 10);
  const m = parseInt(parts[1], 10);
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
}
