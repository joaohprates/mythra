"use client";

import { motion } from "framer-motion";
import {
  Pause, Play, Rewind, FastForward, Volume2, VolumeX, Gauge,
  Music, AlertCircle, Loader2,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState, useCallback } from "react";
import { cn } from "@/lib/cn";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import type { AudioItemDetail } from "@/lib/types";

interface Props {
  audio: AudioItemDetail;
}

// Fetch audio as blob so the Bearer token is included in the request.
// Native <audio src="..."> does not send Authorization headers.
async function fetchAudioBlob(url: string, token: string): Promise<string> {
  const absolute = url.startsWith("http") ? url : window.location.origin + url;
  const res = await fetch(absolute, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error(`Audio fetch failed: ${res.status}`);
  const blob = await res.blob();
  return URL.createObjectURL(blob);
}

export function AudioPlayer({ audio }: Props) {
  const accessToken = useAuthStore((s) => s.accessToken);
  const t = useTranslation();
  const audioRef = useRef<HTMLAudioElement>(null);
  const blobUrlRef = useRef<string | null>(null);

  const [chapterIndex, setChapterIndex] = useState(0);
  const [playing, setPlaying] = useState(false);
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);
  const [muted, setMuted] = useState(false);
  const [volume, setVolume] = useState(1);
  const [speed, setSpeed] = useState(1);
  const [loadingBlob, setLoadingBlob] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const chapter = audio.chapters[chapterIndex];
  const isMusic = audio.audioKind === "Music" || audio.audioKind === "Soundtrack";
  const hasChapters = audio.chapters.length > 0;

  const rawChapterUrl = useMemo(() => {
    if (!chapter) return "";
    return `/api/v1/items/${audio.id}/chapters/${chapter.id}/stream`;
  }, [audio.id, chapter]);

  // Load audio as blob whenever the chapter/source changes
  const loadAudio = useCallback(async (url: string) => {
    if (!url || !accessToken) return;
    const a = audioRef.current;
    if (!a) return;

    setLoadingBlob(true);
    setError(null);
    setPlaying(false);
    setPosition(0);
    setDuration(0);

    // Revoke previous blob to avoid memory leaks
    if (blobUrlRef.current) {
      URL.revokeObjectURL(blobUrlRef.current);
      blobUrlRef.current = null;
    }

    try {
      const blobUrl = await fetchAudioBlob(url, accessToken);
      blobUrlRef.current = blobUrl;
      a.src = blobUrl;
      a.load();
    } catch (err) {
      setError(t("player.audio.error"));
      console.error("Audio load error:", err);
    } finally {
      setLoadingBlob(false);
    }
  }, [accessToken, t]);

  useEffect(() => {
    if (rawChapterUrl) loadAudio(rawChapterUrl);
  }, [rawChapterUrl, loadAudio]);

  // Cleanup blob URLs on unmount
  useEffect(() => {
    return () => {
      if (blobUrlRef.current) URL.revokeObjectURL(blobUrlRef.current);
    };
  }, []);

  useEffect(() => {
    const a = audioRef.current;
    if (!a) return;
    const onTime  = () => setPosition(a.currentTime);
    const onMeta  = () => setDuration(a.duration);
    const onPlay  = () => setPlaying(true);
    const onPause = () => setPlaying(false);
    const onEnd   = () => setChapterIndex((i) => Math.min(audio.chapters.length - 1, i + 1));
    const onError = () => setError(t("player.audio.error"));
    a.addEventListener("timeupdate",     onTime);
    a.addEventListener("loadedmetadata", onMeta);
    a.addEventListener("play",           onPlay);
    a.addEventListener("pause",          onPause);
    a.addEventListener("ended",          onEnd);
    a.addEventListener("error",          onError);
    return () => {
      a.removeEventListener("timeupdate",     onTime);
      a.removeEventListener("loadedmetadata", onMeta);
      a.removeEventListener("play",           onPlay);
      a.removeEventListener("pause",          onPause);
      a.removeEventListener("ended",          onEnd);
      a.removeEventListener("error",          onError);
    };
  }, [audio.chapters.length, t]);

  useEffect(() => {
    if (!audioRef.current) return;
    audioRef.current.playbackRate = speed;
  }, [speed]);

  const toggle = () => {
    const a = audioRef.current;
    if (!a || loadingBlob) return;
    if (a.paused) a.play().catch(() => setError(t("player.audio.error")));
    else a.pause();
  };

  const seek = (s: number) => {
    if (!audioRef.current) return;
    audioRef.current.currentTime = Math.max(0, Math.min(duration, s));
  };

  const cycleSpeed = () =>
    setSpeed((s) => (s === 1 ? 1.25 : s === 1.25 ? 1.5 : s === 1.5 ? 2 : 1));

  return (
    <div className="relative mx-auto max-w-4xl">
      {/* Hidden audio element — src set programmatically via blob URL */}
      <audio ref={audioRef} preload="auto" />

      <motion.div
        initial={{ opacity: 0, y: 24, filter: "blur(8px)" }}
        animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
        transition={{ duration: 0.7, ease: [0.16, 1, 0.3, 1] }}
        className="grid gap-8 md:grid-cols-[280px_1fr]"
      >
        {/* Cover art */}
        <motion.div
          animate={playing ? { rotate: isMusic ? 360 : 0 } : { rotate: 0 }}
          transition={playing && isMusic ? { duration: 20, repeat: Infinity, ease: "linear" } : { duration: 0.4 }}
          className="relative aspect-square overflow-hidden rounded-3xl border border-white/[0.06] shadow-mythra-card"
        >
          {audio.coverPath || audio.posterPath ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={audio.coverPath ?? audio.posterPath ?? ""}
              alt={audio.title}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="grid h-full w-full place-items-center bg-gradient-to-br from-cyan-500/30 via-blue-500/20 to-purple-500/30">
              <Music size={64} className="text-white/30" />
            </div>
          )}
        </motion.div>

        <div className="flex flex-col">
          <span className="text-xs uppercase tracking-widest text-mythra-text-soft">
            {audio.audioKind}
          </span>
          <h1 className="mt-2 text-3xl font-bold tracking-tight md:text-4xl">
            <span className="gradient-text">{audio.title}</span>
          </h1>
          {audio.author && (
            <p className="mt-1 text-sm text-mythra-text-muted">
              {audio.author}
              {audio.narrator && ` · ${audio.narrator}`}
            </p>
          )}

          {/* Error state */}
          {error && (
            <div className="mt-4 flex items-center gap-2 rounded-xl border border-rose-500/20 bg-rose-500/10 px-3 py-2 text-sm text-rose-300">
              <AlertCircle size={14} />
              {error}
            </div>
          )}

          {/* Progress bar */}
          <div className="mt-6">
            {hasChapters && (
              <p className="text-sm font-semibold text-white">{chapter?.title ?? "—"}</p>
            )}
            <div className="relative mt-2">
              <input
                type="range"
                min={0}
                max={duration || 0}
                step={1}
                value={position}
                onChange={(e) => seek(Number(e.target.value))}
                disabled={loadingBlob || !!error}
                className="w-full accent-mythra-purple disabled:opacity-50"
              />
            </div>
            <div className="flex justify-between text-xs text-mythra-text-soft">
              <span>{formatTime(position)}</span>
              <span>{formatTime(duration)}</span>
            </div>
          </div>

          {/* Controls */}
          <div className="mt-4 flex flex-wrap items-center gap-2">
            <ControlButton onClick={() => seek(position - 15)} aria-label="Back 15s" disabled={loadingBlob}>
              <Rewind size={16} /> 15
            </ControlButton>

            <PrimaryButton onClick={toggle} disabled={loadingBlob || !!error}>
              {loadingBlob
                ? <Loader2 size={20} className="animate-spin" />
                : playing
                  ? <Pause size={20} />
                  : <Play size={20} className="fill-current" />
              }
            </PrimaryButton>

            <ControlButton onClick={() => seek(position + 30)} aria-label="Forward 30s" disabled={loadingBlob}>
              <FastForward size={16} /> 30
            </ControlButton>

            <button
              onClick={() => setMuted((m) => {
                if (audioRef.current) audioRef.current.muted = !m;
                return !m;
              })}
              className="grid h-10 w-10 place-items-center rounded-full bg-white/[0.05] hover:bg-white/[0.12] transition-colors"
              aria-label="Toggle mute"
            >
              {muted ? <VolumeX size={16} /> : <Volume2 size={16} />}
            </button>

            <input
              type="range"
              min={0}
              max={1}
              step={0.01}
              value={muted ? 0 : volume}
              onChange={(e) => {
                const v = Number(e.target.value);
                setVolume(v);
                if (audioRef.current) audioRef.current.volume = v;
              }}
              className="h-1 w-24 accent-mythra-purple"
            />

            <button
              onClick={cycleSpeed}
              className="ml-auto inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1 text-xs hover:bg-white/10 transition-colors"
            >
              <Gauge size={14} /> {speed}×
            </button>
          </div>

          {/* Chapter list */}
          {hasChapters && (
            <div className="mt-8">
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-mythra-text-soft">
                {t("player.chapters")}
              </h3>
              <ul className="space-y-1 max-h-64 overflow-y-auto pr-1">
                {audio.chapters.map((c, i) => (
                  <li key={c.id}>
                    <button
                      onClick={() => setChapterIndex(i)}
                      className={cn(
                        "w-full rounded-xl px-3 py-2 text-left text-sm transition-colors",
                        i === chapterIndex
                          ? "bg-gradient-to-r from-mythra-purple/30 to-mythra-blue/15 text-white"
                          : "text-mythra-text-muted hover:bg-white/[0.04] hover:text-white"
                      )}
                    >
                      <span className="mr-2 inline-block w-6 text-right opacity-60">{i + 1}</span>
                      {c.title}
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {!hasChapters && (
            <p className="mt-6 text-sm text-mythra-text-soft">{t("player.noChapters")}</p>
          )}
        </div>
      </motion.div>
    </div>
  );
}

function ControlButton({
  children, onClick, disabled, "aria-label": ariaLabel,
}: {
  children: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
  "aria-label"?: string;
}) {
  return (
    <motion.button
      whileHover={disabled ? {} : { scale: 1.05 }}
      whileTap={disabled ? {} : { scale: 0.94 }}
      onClick={onClick}
      disabled={disabled}
      aria-label={ariaLabel}
      className="inline-flex h-10 items-center gap-1 rounded-full bg-white/[0.05] px-3 text-xs text-white hover:bg-white/[0.12] disabled:opacity-40 transition-colors"
    >
      {children}
    </motion.button>
  );
}

function PrimaryButton({ children, onClick, disabled }: { children: React.ReactNode; onClick: () => void; disabled?: boolean }) {
  return (
    <motion.button
      whileHover={disabled ? {} : { scale: 1.06 }}
      whileTap={disabled ? {} : { scale: 0.94 }}
      onClick={onClick}
      disabled={disabled}
      className="grid h-12 w-12 place-items-center rounded-full bg-white text-black shadow-lg hover:bg-white/95 disabled:opacity-40 transition-all"
    >
      {children}
    </motion.button>
  );
}

function formatTime(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds < 0) return "0:00";
  const total = Math.floor(seconds);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  return h > 0
    ? `${h}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`
    : `${m}:${s.toString().padStart(2, "0")}`;
}
