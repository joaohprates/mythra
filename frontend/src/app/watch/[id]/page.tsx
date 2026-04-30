"use client";

import { motion } from "framer-motion";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useRef } from "react";
import { ChevronLeft } from "lucide-react";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { VideoPlayer } from "@/components/media/VideoPlayer";
import { ExternalPlayer } from "@/components/media/ExternalPlayer";
import { PlayIMDBPlayer } from "@/components/media/PlayIMDBPlayer";
import type { VideoItemDetail } from "@/lib/types";

export default function WatchPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const profile = useAuthStore((s) => s.activeProfile);
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const item = useQuery({
    queryKey: ["video-detail", params.id],
    queryFn: async () => (await api.get<VideoItemDetail>(`/items/${params.id}`)).data,
    enabled: !!params.id && !!accessToken,
  });

  const initial = useQuery({
    queryKey: ["playback", profile?.id, params.id],
    queryFn: async () => {
      if (!profile) return null;
      const r = await api.get(`/profiles/${profile.id}/playback/${params.id}`);
      return r.data;
    },
    enabled: !!profile,
  });

  const lastSent = useRef(0);
  const handleProgress = useCallback(
    (position: number, duration: number) => {
      if (!profile) return;
      const now = Date.now();
      if (now - lastSent.current < 5000) return;
      lastSent.current = now;
      api
        .put(`/profiles/${profile.id}/playback/${params.id}`, {
          position: secondsToTimespan(position),
          duration: secondsToTimespan(duration),
        })
        .catch(() => {});
    },
    [profile, params.id]
  );

  if (!profile) return <div className="grid min-h-screen place-items-center text-mythra-text-muted">Choose a profile first.</div>;

  const hasFile = item.data?.hasFile ?? true; // default true so existing items work before fetch
  const imdbId = item.data?.imdbId;
  // Priority: local file > PlayIMDB (when IMDB ID known) > other external providers
  const usePlayImdb = !hasFile && !!imdbId && !!item.data;
  const isExternalOnly = item.data && !hasFile && !usePlayImdb;

  return (
    <main className="relative min-h-screen bg-black">
      <motion.button
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        onClick={() => router.back()}
        className="group absolute left-6 top-6 z-30 flex items-center gap-2 rounded-full border border-white/10 bg-black/40 backdrop-blur px-4 py-2 text-sm text-white/80 transition hover:bg-white/10"
      >
        <ChevronLeft size={16} className="transition-transform group-hover:-translate-x-0.5" />
        Back
      </motion.button>

      <div className="mx-auto flex min-h-screen max-w-[1700px] flex-col px-4 pt-20 lg:px-10">

        {/* ── Player ─────────────────────────────────────────────────────── */}
        {usePlayImdb ? (
          <PlayIMDBPlayer imdbId={imdbId!} title={item.data?.title ?? ""} />
        ) : isExternalOnly ? (
          <ExternalPlayer
            mediaItemId={params.id}
            season={item.data?.seasonNumber}
            episode={item.data?.episodeNumber}
            title={item.data?.title ?? ""}
          />
        ) : (
          <VideoPlayer
            videoItemId={params.id}
            profileId={profile.id}
            initialPositionSeconds={timespanToSeconds(initial.data?.position)}
            onProgress={handleProgress}
          />
        )}

        {/* ── Metadata ───────────────────────────────────────────────────── */}
        {item.data && (
          <div className="mt-8 grid gap-6 md:grid-cols-[1fr_320px]">
            <div>
              <h1 className="text-3xl font-bold tracking-tight md:text-4xl">{item.data.title}</h1>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-mythra-text-soft">
                {item.data.year && <span>{item.data.year}</span>}
                {!isExternalOnly && (
                  <span className="rounded-full border border-white/10 px-2 py-0.5">{item.data.resolutionLabel}</span>
                )}
                {item.data.duration && <span>{formatDuration(item.data.duration)}</span>}
                {(item.data.genres ?? []).slice(0, 3).map((g) => (
                  <span key={g} className="rounded-full border border-white/10 px-2 py-0.5">{g}</span>
                ))}
                {usePlayImdb && (
                  <span className="rounded-full border border-mythra-purple/40 bg-mythra-purple/10 px-2 py-0.5 text-mythra-purple">
                    Streaming via PlayIMDB
                  </span>
                )}
                {isExternalOnly && !usePlayImdb && (
                  <span className="rounded-full border border-mythra-purple/40 bg-mythra-purple/10 px-2 py-0.5 text-mythra-purple">
                    Streaming via external provider
                  </span>
                )}
              </div>
              {item.data.overview && <p className="mt-4 text-sm leading-relaxed text-mythra-text-muted">{item.data.overview}</p>}
            </div>

            {!isExternalOnly && (
              <aside className="rounded-2xl border border-white/[0.06] bg-white/[0.03] p-5 backdrop-blur">
                <h3 className="mb-3 text-sm font-semibold uppercase tracking-widest text-mythra-text-soft">Streams</h3>
                <div className="space-y-3">
                  <Group label="Audio">
                    {(item.data.audioTracks ?? []).map((a) => (
                      <li key={a.id}>{a.languageCode.toUpperCase()} • {a.codec} • {a.channelLayout}</li>
                    ))}
                  </Group>
                  <Group label="Subtitles">
                    {(item.data.subtitles ?? []).length === 0 ? (
                      <li className="text-mythra-text-soft">None embedded</li>
                    ) : (
                      (item.data.subtitles ?? []).map((s) => (
                        <li key={s.id}>{s.languageCode.toUpperCase()} • {s.format}</li>
                      ))
                    )}
                  </Group>
                </div>
                <Link
                  href={`/item/${params.id}`}
                  className="mt-4 inline-flex items-center gap-2 text-xs text-mythra-text-soft hover:text-white"
                >
                  View full details →
                </Link>
              </aside>
            )}
          </div>
        )}
      </div>
    </main>
  );
}

function Group({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-1.5 text-[11px] uppercase tracking-wider text-mythra-text-soft">{label}</p>
      <ul className="space-y-1 text-xs text-white/80">{children}</ul>
    </div>
  );
}

function timespanToSeconds(ts?: string | null): number {
  if (!ts) return 0;
  const parts = ts.split(":");
  if (parts.length !== 3) return 0;
  return parseFloat(parts[0]) * 3600 + parseFloat(parts[1]) * 60 + parseFloat(parts[2]);
}

function secondsToTimespan(seconds: number): string {
  if (!Number.isFinite(seconds)) return "00:00:00";
  const total = Math.floor(seconds);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
}

function formatDuration(ts: string): string {
  const seconds = timespanToSeconds(ts);
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
}
