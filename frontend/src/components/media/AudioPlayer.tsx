"use client";

import { motion } from "framer-motion";
import { Pause, Play, Rewind, FastForward, Volume2, VolumeX, Gauge } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/cn";
import type { AudioItemDetail } from "@/lib/types";

interface Props {
  audio: AudioItemDetail;
}

export function AudioPlayer({ audio }: Props) {
  const audioRef = useRef<HTMLAudioElement>(null);
  const [chapterIndex, setChapterIndex] = useState(0);
  const [playing, setPlaying] = useState(false);
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);
  const [muted, setMuted] = useState(false);
  const [volume, setVolume] = useState(1);
  const [speed, setSpeed] = useState(1);
  const chapter = audio.chapters[chapterIndex];

  const chapterUrl = useMemo(() => {
    if (!chapter) return "";
    return `/api/v1/items/${audio.id}/chapters/${chapter.id}/stream`;
  }, [audio.id, chapter]);

  useEffect(() => {
    const a = audioRef.current;
    if (!a) return;
    const onTime = () => setPosition(a.currentTime);
    const onMeta = () => setDuration(a.duration);
    const onPlay = () => setPlaying(true);
    const onPause = () => setPlaying(false);
    const onEnd = () => setChapterIndex((i) => Math.min(audio.chapters.length - 1, i + 1));
    a.addEventListener("timeupdate", onTime);
    a.addEventListener("loadedmetadata", onMeta);
    a.addEventListener("play", onPlay);
    a.addEventListener("pause", onPause);
    a.addEventListener("ended", onEnd);
    return () => {
      a.removeEventListener("timeupdate", onTime);
      a.removeEventListener("loadedmetadata", onMeta);
      a.removeEventListener("play", onPlay);
      a.removeEventListener("pause", onPause);
      a.removeEventListener("ended", onEnd);
    };
  }, [audio.chapters.length]);

  useEffect(() => {
    if (!audioRef.current) return;
    audioRef.current.playbackRate = speed;
  }, [speed]);

  const toggle = () => {
    const a = audioRef.current;
    if (!a) return;
    if (a.paused) a.play();
    else a.pause();
  };

  const seek = (s: number) => {
    if (!audioRef.current) return;
    audioRef.current.currentTime = Math.max(0, Math.min(duration, s));
  };

  return (
    <div className="relative mx-auto max-w-4xl">
      <audio ref={audioRef} src={chapterUrl} preload="metadata" />

      <motion.div
        initial={{ opacity: 0, y: 24, filter: "blur(8px)" }}
        animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
        transition={{ duration: 0.7, ease: [0.16, 1, 0.3, 1] }}
        className="grid gap-8 md:grid-cols-[280px_1fr]"
      >
        <motion.div
          animate={playing ? { rotate: 360 } : { rotate: 0 }}
          transition={playing ? { duration: 30, repeat: Infinity, ease: "linear" } : { duration: 0.4 }}
          className="relative aspect-square overflow-hidden rounded-3xl border border-white/[0.06] shadow-mythra-card"
        >
          {audio.coverPath ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={audio.coverPath} alt={audio.title} className="h-full w-full object-cover" />
          ) : (
            <div className="grid h-full w-full place-items-center bg-gradient-to-br from-cyan-500/30 via-blue-500/20 to-purple-500/30 text-mythra-text-soft">
              {audio.title}
            </div>
          )}
        </motion.div>

        <div className="flex flex-col">
          <span className="text-xs uppercase tracking-widest text-mythra-text-soft">{audio.audioKind}</span>
          <h1 className="mt-2 text-3xl font-bold tracking-tight md:text-4xl"><span className="gradient-text">{audio.title}</span></h1>
          {audio.author && <p className="mt-1 text-sm text-mythra-text-muted">{audio.author}{audio.narrator && ` • narrated by ${audio.narrator}`}</p>}

          <div className="mt-6">
            <p className="text-sm font-semibold">{chapter?.title ?? "—"}</p>
            <input
              type="range"
              min={0}
              max={duration || 0}
              step={1}
              value={position}
              onChange={(e) => seek(Number(e.target.value))}
              className="mt-2 w-full accent-mythra-purple"
            />
            <div className="flex justify-between text-xs text-mythra-text-soft">
              <span>{formatTime(position)}</span>
              <span>{formatTime(duration)}</span>
            </div>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-2">
            <ControlButton onClick={() => seek(position - 15)} aria-label="Back 15s">
              <Rewind size={18} /> 15
            </ControlButton>
            <PrimaryButton onClick={toggle}>
              {playing ? <Pause size={20} /> : <Play size={20} className="fill-current" />}
            </PrimaryButton>
            <ControlButton onClick={() => seek(position + 30)} aria-label="Forward 30s">
              <FastForward size={18} /> 30
            </ControlButton>
            <button
              onClick={() => setMuted((m) => { if (audioRef.current) audioRef.current.muted = !m; return !m; })}
              className="grid h-10 w-10 place-items-center rounded-full bg-white/[0.05] hover:bg-white/[0.12]"
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
              onClick={() => setSpeed((s) => (s === 1 ? 1.25 : s === 1.25 ? 1.5 : s === 1.5 ? 2 : 1))}
              className="ml-auto inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1 text-xs hover:bg-white/10"
            >
              <Gauge size={14} /> {speed}x
            </button>
          </div>

          <div className="mt-8">
            <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-mythra-text-soft">Chapters</h3>
            <ul className="space-y-1">
              {audio.chapters.map((c, i) => (
                <li key={c.id}>
                  <button
                    onClick={() => setChapterIndex(i)}
                    className={cn(
                      "w-full rounded-xl px-3 py-2 text-left text-sm transition",
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
        </div>
      </motion.div>
    </div>
  );
}

function ControlButton({
  children,
  onClick,
  "aria-label": ariaLabel,
}: {
  children: React.ReactNode;
  onClick: () => void;
  "aria-label"?: string;
}) {
  return (
    <motion.button
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.94 }}
      onClick={onClick}
      aria-label={ariaLabel}
      className="inline-flex h-10 items-center gap-1 rounded-full bg-white/[0.05] px-3 text-xs text-white hover:bg-white/[0.12]"
    >
      {children}
    </motion.button>
  );
}

function PrimaryButton({ children, onClick }: { children: React.ReactNode; onClick: () => void }) {
  return (
    <motion.button
      whileHover={{ scale: 1.06 }}
      whileTap={{ scale: 0.94 }}
      onClick={onClick}
      className="grid h-12 w-12 place-items-center rounded-full bg-white text-black shadow-lg hover:bg-white/95"
    >
      {children}
    </motion.button>
  );
}

function formatTime(seconds: number): string {
  if (!Number.isFinite(seconds)) return "0:00";
  const total = Math.floor(seconds);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  return h > 0
    ? `${h}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`
    : `${m}:${s.toString().padStart(2, "0")}`;
}
