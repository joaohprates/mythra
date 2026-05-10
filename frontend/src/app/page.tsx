"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Clapperboard, FolderOpen, Loader2, Sparkles } from "lucide-react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { HeroBanner } from "@/components/media/HeroBanner";
import { ContentRow } from "@/components/media/ContentRow";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import type { MediaItem, PlaybackProgress, ReadingProgressDto } from "@/lib/types";

type RecommendationItem = {
  id: string;
  kind: string;
  title: string;
  posterPath?: string | null;
  backdropPath?: string | null;
  rating?: number | null;
  year?: number | null;
  genres: string[];
  reason: string;
};

const HOME_QUERY_OPTS = {
  staleTime: 60_000,
  refetchOnMount: "always" as const,
  refetchOnWindowFocus: false,
  retry: 2,
};

export default function HomePage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const profile = useAuthStore((s) => s.activeProfile);
  const qc = useQueryClient();
  const t = useTranslation();

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  // Refresh stale persisted query cache once after hydration completes.
  const didInvalidateRef = useRef(false);
  useEffect(() => {
    if (didInvalidateRef.current) return;
    if (isHydrated && accessToken) {
      didInvalidateRef.current = true;
      qc.invalidateQueries();
    }
  }, [isHydrated, accessToken, qc]);

  const tokenReady = isHydrated && !!accessToken;
  const profileReady = isHydrated && !!profile;

  const trendingMovies = useQuery({
    queryKey: ["videos", "movies"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", take: 18, includeAdult: true } })).data.items,
    enabled: tokenReady,
    ...HOME_QUERY_OPTS,
  });

  const animeRow = useQuery({
    queryKey: ["videos", "anime"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", search: "anime", take: 18, includeAdult: true } })).data.items,
    enabled: tokenReady,
    ...HOME_QUERY_OPTS,
  });

  const mangaRow = useQuery({
    queryKey: ["mangas"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Manga", take: 18, includeAdult: true } })).data.items,
    enabled: tokenReady,
    ...HOME_QUERY_OPTS,
  });

  const bookRow = useQuery({
    queryKey: ["books"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Book", take: 18, includeAdult: true } })).data.items,
    enabled: tokenReady,
    ...HOME_QUERY_OPTS,
  });

  const continueWatching = useQuery({
    queryKey: ["continue-watching", profile?.id],
    queryFn: async () => {
      if (!profile) return [];
      const progress = (await api.get<PlaybackProgress[]>(`/profiles/${profile.id}/continue-watching`)).data;
      if (progress.length === 0) return [];
      const ids = progress.map((p) => p.mediaItemId).join(",");
      const result = (await api.get<{ items: MediaItem[] }>("/items", { params: { ids, take: progress.length } })).data;
      const itemMap = new Map(result.items.map((i) => [i.id, i]));
      return progress.map((p) => itemMap.get(p.mediaItemId)).filter((i): i is MediaItem => !!i);
    },
    enabled: profileReady,
    ...HOME_QUERY_OPTS,
  });

  const continueReading = useQuery({
    queryKey: ["continue-reading", profile?.id],
    queryFn: async () => {
      if (!profile) return [];
      return (await api.get<ReadingProgressDto[]>(`/profiles/${profile.id}/continue-reading`)).data;
    },
    enabled: profileReady,
    ...HOME_QUERY_OPTS,
  });

  const recommendations = useQuery({
    queryKey: ["recommendations", profile?.id],
    queryFn: async () => {
      if (!profile) return [];
      return (await api.get<RecommendationItem[]>("/recommendations", { params: { profileId: profile.id, take: 18 } })).data;
    },
    enabled: profileReady,
    ...HOME_QUERY_OPTS,
  });

  // Hero fallback: walk rows for first non-empty source.
  const heroSource =
    (trendingMovies.data && trendingMovies.data.length > 0 && trendingMovies.data) ||
    (animeRow.data && animeRow.data.length > 0 && animeRow.data) ||
    (mangaRow.data && mangaRow.data.length > 0 && mangaRow.data) ||
    (bookRow.data && bookRow.data.length > 0 && bookRow.data) ||
    [];
  const heroItems = heroSource.slice(0, 5);

  // Reference unused query so lint stays happy and data still warms the cache.
  void continueReading;

  return (
    <>
      <Topbar />
      <PageScaffold className="pt-0">
        <HeroBanner items={heroItems} />

        <div className="space-y-12">
          {continueWatching.data && continueWatching.data.length > 0 && (
            <ContentRow title={t("home.row.continueWatching")} subtitle={t("home.row.continueWatching.sub")} items={continueWatching.data} size="md" />
          )}
          {(recommendations.data?.length ?? 0) > 0 && (
            <ContentRow
              title={t("home.row.recommended")}
              subtitle={recommendations.data?.[0]?.reason}
              items={(recommendations.data ?? []).map((r) => ({
                id: r.id,
                kind: r.kind as MediaItem["kind"],
                title: r.title,
                posterPath: r.posterPath,
                backdropPath: r.backdropPath,
                rating: r.rating,
                genres: r.genres,
                tags: [],
                createdAt: "",
                libraryId: "",
              } satisfies MediaItem))}
              size="md"
            />
          )}
          <ContentRow title={t("home.row.movies")} items={trendingMovies.data ?? []} size="md" loading={trendingMovies.isLoading} />
          {(animeRow.data?.length ?? 0) > 0 && <ContentRow title={t("home.row.anime")} items={animeRow.data ?? []} size="md" />}
          {(mangaRow.data?.length ?? 0) > 0 && <ContentRow title={t("home.row.manga")} items={mangaRow.data ?? []} size="sm" />}
          {(bookRow.data?.length ?? 0) > 0 && <ContentRow title={t("home.row.books")} items={bookRow.data ?? []} size="sm" />}
        </div>

        {!trendingMovies.isLoading && (trendingMovies.data?.length ?? 0) === 0 && (
          <EmptyState onDemoLoaded={() => qc.invalidateQueries()} />
        )}
      </PageScaffold>
    </>
  );
}

function EmptyState({ onDemoLoaded }: { onDemoLoaded: () => void }) {
  const [loading, setLoading] = useState(false);
  const [done, setDone] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const t = useTranslation();

  const loadDemo = async () => {
    setLoading(true);
    setErr(null);
    try {
      await api.post("/seed/demo");
      setDone(true);
      setTimeout(onDemoLoaded, 800);
    } catch (e) {
      const msg = (e as { response?: { data?: { detail?: string } } }).response?.data?.detail;
      setErr(msg ?? t("home.empty.demoError"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Quick-start: demo button */}
      <div className="rounded-3xl border border-mythra-purple/20 bg-mythra-purple/5 p-8 text-center backdrop-blur">
        <span className="mx-auto mb-4 grid h-14 w-14 place-items-center rounded-2xl bg-gradient-to-br from-mythra-purple via-mythra-blue to-mythra-magenta mythra-glow-purple">
          <Clapperboard size={24} className="text-white" />
        </span>
        <h3 className="text-2xl font-semibold gradient-text">{t("home.empty.title")}</h3>
        <p className="mt-3 text-sm text-mythra-text-muted">
          {t("home.empty.body")}
        </p>

        {done ? (
          <p className="mt-5 flex items-center justify-center gap-2 text-sm text-emerald-400">
            <Sparkles size={16} /> {t("home.empty.demoLoaded")}
          </p>
        ) : (
          <>
            <button
              onClick={loadDemo}
              disabled={loading}
              className="mt-6 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple via-[#7c3aed] to-mythra-blue px-6 py-3 text-sm font-semibold text-white shadow-[0_18px_40px_-15px_rgba(168,85,247,0.7)] transition hover:scale-[1.02] disabled:cursor-wait disabled:opacity-70"
            >
              {loading ? <Loader2 size={16} className="animate-spin" /> : <Sparkles size={16} />}
              {loading ? t("home.empty.demoLoading") : t("home.empty.demoCta")}
            </button>
            {err && <p className="mt-3 text-xs text-rose-300">{err}</p>}
          </>
        )}
      </div>

      {/* Setup guide: local files */}
      <div className="rounded-3xl border border-white/[0.06] bg-white/[0.02] p-8 backdrop-blur">
        <div className="flex items-start gap-4">
          <span className="mt-0.5 grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-white/[0.05]">
            <FolderOpen size={18} className="text-mythra-text-muted" />
          </span>
          <div>
            <h4 className="font-semibold">{t("home.empty.localTitle")}</h4>
            <p className="mt-1 text-sm text-mythra-text-muted">
              {t("home.empty.localBody")}
            </p>
            <ul className="mt-3 space-y-1 text-xs text-mythra-text-soft">
              <li>🎬 <strong>Videos:</strong> .mp4 .mkv .m4v .mov .avi .webm</li>
              <li>📚 <strong>Books:</strong> .epub .pdf .mobi .azw3</li>
              <li>🖼 <strong>Manga:</strong> .cbz .cbr archives in per-series folders</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
