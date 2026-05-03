"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Clapperboard, FolderOpen, Loader2, Sparkles } from "lucide-react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { HeroBanner } from "@/components/media/HeroBanner";
import { ContentRow } from "@/components/media/ContentRow";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useProfilePrefs } from "@/store/profile";
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

export default function HomePage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const profile = useAuthStore((s) => s.activeProfile);
  const qc = useQueryClient();

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const { showAdultContent } = useProfilePrefs();

  // When adult content is disabled, explicitly filter it out from all queries
  const adultFilter = showAdultContent ? undefined : false;

  const recentVideo = useQuery({
    queryKey: ["recent", "Video", adultFilter],
    queryFn: async () =>
      (await api.get<{ items: MediaItem[] }>("/items", { params: { take: 18, orderBy: "-added", isAdult: adultFilter } })).data.items,
    enabled: !!accessToken,
  });

  const trendingMovies = useQuery({
    queryKey: ["videos", "movies", adultFilter],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", take: 18, isAdult: adultFilter } })).data.items,
    enabled: !!accessToken,
  });

  const animeRow = useQuery({
    queryKey: ["videos", "anime", adultFilter],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", search: "anime", take: 18, isAdult: adultFilter } })).data.items,
    enabled: !!accessToken,
  });

  const mangaRow = useQuery({
    queryKey: ["mangas", adultFilter],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Manga", take: 18, isAdult: adultFilter } })).data.items,
    enabled: !!accessToken,
  });

  const bookRow = useQuery({
    queryKey: ["books", adultFilter],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Book", take: 18, isAdult: adultFilter } })).data.items,
    enabled: !!accessToken,
  });

  const audioRow = useQuery({
    queryKey: ["audio", adultFilter],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Audio", take: 18, isAdult: adultFilter } })).data.items,
    enabled: !!accessToken,
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
    enabled: !!profile,
  });

  const continueReading = useQuery({
    queryKey: ["continue-reading", profile?.id],
    queryFn: async () => {
      if (!profile) return [];
      return (await api.get<ReadingProgressDto[]>(`/profiles/${profile.id}/continue-reading`)).data;
    },
    enabled: !!profile,
  });

  const recommendations = useQuery({
    queryKey: ["recommendations", profile?.id],
    queryFn: async () => {
      if (!profile) return [];
      return (await api.get<RecommendationItem[]>("/recommendations", { params: { profileId: profile.id, take: 18 } })).data;
    },
    enabled: !!profile,
  });

  const heroItems = (recentVideo.data ?? []).slice(0, 5);

  return (
    <>
      <Topbar />
      <PageScaffold className="pt-0">
        <HeroBanner items={heroItems} />

        <div className="space-y-12">
          {continueWatching.data && continueWatching.data.length > 0 && (
            <ContentRow title="Continue Watching" subtitle="Continue de onde parou" items={continueWatching.data} size="md" />
          )}
          {(recommendations.data?.length ?? 0) > 0 && (
            <ContentRow
              title="Recomendado Para Você"
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
          <ContentRow title="Adicionados Recentemente" subtitle="Novidades no seu universo" items={recentVideo.data ?? []} size="md" loading={recentVideo.isLoading} />
          <ContentRow title="Filmes & Séries" items={trendingMovies.data ?? []} size="md" loading={trendingMovies.isLoading} />
          {(animeRow.data?.length ?? 0) > 0 && <ContentRow title="Anime" items={animeRow.data ?? []} size="md" />}
          {(mangaRow.data?.length ?? 0) > 0 && <ContentRow title="Mangá" items={mangaRow.data ?? []} size="sm" />}
          {(bookRow.data?.length ?? 0) > 0 && <ContentRow title="Livros" items={bookRow.data ?? []} size="sm" />}
          {(audioRow.data?.length ?? 0) > 0 && <ContentRow title="Music" items={audioRow.data ?? []} size="md" />}
        </div>

        {!recentVideo.isLoading && (recentVideo.data?.length ?? 0) === 0 && (
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

  const loadDemo = async () => {
    setLoading(true);
    setErr(null);
    try {
      await api.post("/seed/demo");
      setDone(true);
      setTimeout(onDemoLoaded, 800);
    } catch (e) {
      const msg = (e as { response?: { data?: { detail?: string } } }).response?.data?.detail;
      setErr(msg ?? "Failed to load demo content.");
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
        <h3 className="text-2xl font-semibold gradient-text">Your universe awaits</h3>
        <p className="mt-3 text-sm text-mythra-text-muted">
          Load a curated set of popular movies instantly — no files needed. They stream via external
          providers so you can explore Mythra right away.
        </p>

        {done ? (
          <p className="mt-5 flex items-center justify-center gap-2 text-sm text-emerald-400">
            <Sparkles size={16} /> Demo library loaded! Refreshing…
          </p>
        ) : (
          <>
            <button
              onClick={loadDemo}
              disabled={loading}
              className="mt-6 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple via-[#7c3aed] to-mythra-blue px-6 py-3 text-sm font-semibold text-white shadow-[0_18px_40px_-15px_rgba(168,85,247,0.7)] transition hover:scale-[1.02] disabled:cursor-wait disabled:opacity-70"
            >
              {loading ? <Loader2 size={16} className="animate-spin" /> : <Sparkles size={16} />}
              {loading ? "Loading…" : "Load demo content"}
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
            <h4 className="font-semibold">Add your own media</h4>
            <p className="mt-1 text-sm text-mythra-text-muted">
              Put your files inside the <code className="rounded bg-white/10 px-1.5 py-0.5 text-xs text-white">media/</code>{" "}
              folder next to <code className="rounded bg-white/10 px-1.5 py-0.5 text-xs text-white">docker-compose.yml</code>,
              then go to{" "}
              <a href="/settings" className="text-mythra-purple underline-offset-2 hover:underline">Settings → Libraries</a>{" "}
              and create a library pointing to <code className="rounded bg-white/10 px-1.5 py-0.5 text-xs text-white">/media</code>.
              Hit <strong>Scan</strong> and your content appears here.
            </p>
            <ul className="mt-3 space-y-1 text-xs text-mythra-text-soft">
              <li>🎬 <strong>Videos:</strong> .mp4 .mkv .m4v .mov .avi .webm</li>
              <li>📚 <strong>Books:</strong> .epub .pdf .mobi .azw3</li>
              <li>🖼 <strong>Manga:</strong> .cbz .cbr archives in per-series folders</li>
              <li>🎧 <strong>Audiobooks:</strong> .mp3 .flac .m4a .ogg</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
