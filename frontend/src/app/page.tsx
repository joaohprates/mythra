"use client";

import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { HeroBanner } from "@/components/media/HeroBanner";
import { ContentRow } from "@/components/media/ContentRow";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import type { MediaItem, PlaybackProgress, ReadingProgressDto } from "@/lib/types";

export default function HomePage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const profile = useAuthStore((s) => s.activeProfile);

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const recentVideo = useQuery({
    queryKey: ["recent", "Video"],
    queryFn: async () => (await api.get<MediaItem[]>("/items/recently-added", { params: { take: 18 } })).data,
    enabled: !!accessToken,
  });

  const trendingMovies = useQuery({
    queryKey: ["videos", "movies"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", take: 18 } })).data.items,
    enabled: !!accessToken,
  });

  const animeRow = useQuery({
    queryKey: ["videos", "anime"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", search: "anime", take: 18 } })).data.items,
    enabled: !!accessToken,
  });

  const mangaRow = useQuery({
    queryKey: ["mangas"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Manga", take: 18 } })).data.items,
    enabled: !!accessToken,
  });

  const bookRow = useQuery({
    queryKey: ["books"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Book", take: 18 } })).data.items,
    enabled: !!accessToken,
  });

  const audioRow = useQuery({
    queryKey: ["audio"],
    queryFn: async () => (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Audio", take: 18 } })).data.items,
    enabled: !!accessToken,
  });

  const continueWatching = useQuery({
    queryKey: ["continue-watching", profile?.id],
    queryFn: async () => {
      if (!profile) return [];
      const progress = (await api.get<PlaybackProgress[]>(`/profiles/${profile.id}/continue-watching`)).data;
      if (progress.length === 0) return [];
      const ids = progress.map((p) => p.mediaItemId).join(",");
      const items = (await api.get<{ items: MediaItem[] }>("/items", { params: { /* TODO: byIds */ } })).data.items;
      return items.filter((i) => progress.some((p) => p.mediaItemId === i.id));
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

  const heroItems = (recentVideo.data ?? []).slice(0, 5);

  return (
    <>
      <Topbar />
      <PageScaffold className="pt-0">
        <HeroBanner items={heroItems} />

        <div className="space-y-12">
          {continueWatching.data && continueWatching.data.length > 0 && (
            <ContentRow title="Continue Watching" subtitle="Pick up where you left off" items={continueWatching.data} size="md" />
          )}
          <ContentRow title="Recently Added" subtitle="Fresh in your universe" items={recentVideo.data ?? []} size="md" loading={recentVideo.isLoading} />
          <ContentRow title="Movies & TV" items={trendingMovies.data ?? []} size="md" loading={trendingMovies.isLoading} />
          {(animeRow.data?.length ?? 0) > 0 && <ContentRow title="Anime" items={animeRow.data ?? []} size="md" />}
          {(mangaRow.data?.length ?? 0) > 0 && <ContentRow title="Manga" items={mangaRow.data ?? []} size="sm" />}
          {(bookRow.data?.length ?? 0) > 0 && <ContentRow title="Books" items={bookRow.data ?? []} size="sm" />}
          {(audioRow.data?.length ?? 0) > 0 && <ContentRow title="Audiobooks" items={audioRow.data ?? []} size="md" />}
        </div>

        {!recentVideo.isLoading && (recentVideo.data?.length ?? 0) === 0 && <EmptyState />}
      </PageScaffold>
    </>
  );
}

function EmptyState() {
  return (
    <div className="mx-auto max-w-xl rounded-3xl border border-white/[0.06] bg-white/[0.03] p-10 text-center backdrop-blur">
      <h3 className="text-2xl font-semibold gradient-text">Your universe awaits</h3>
      <p className="mt-3 text-sm text-mythra-text-muted">
        No libraries indexed yet. Head to settings to add a folder of movies, manga, books, or audiobooks — and watch
        Mythra come alive.
      </p>
    </div>
  );
}
