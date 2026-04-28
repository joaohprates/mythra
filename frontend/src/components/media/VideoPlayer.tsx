"use client";

import { motion, AnimatePresence } from "framer-motion";
import Hls from "hls.js";
import {
  Maximize,
  Minimize,
  Pause,
  Play,
  SkipBack,
  SkipForward,
  Volume2,
  VolumeX,
} from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { cn } from "@/lib/cn";
import { api, streamUrl } from "@/lib/api";

interface Props {
  videoItemId: string;
  profileId: string;
  initialPositionSeconds?: number;
  onProgress?: (positionSeconds: number, durationSeconds: number) => void;
  onEnded?: () => void;
}

export function VideoPlayer({ videoItemId, profileId, initialPositionSeconds = 0, onProgress, onEnded }: Props) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const hlsRef = useRef<Hls | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [muted, setMuted] = useState(false);
  const [volume, setVolume] = useState(1);
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);
  const [showControls, setShowControls] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [bufferedEnd, setBufferedEnd] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const hideTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Start a stream session
  useEffect(() => {
    let cancelled = false;
    api
      .post("/stream/start", { mediaItemId: videoItemId }, { params: { profileId } })
      .then((res) => {
        if (!cancelled) setToken(res.data.sessionToken);
      })
      .catch((e) => setError(e?.message ?? "Failed to start stream"));
    return () => {
      cancelled = true;
    };
  }, [videoItemId, profileId]);

  // Wire up HLS
  useEffect(() => {
    if (!token || !videoRef.current) return;
    const url = streamUrl(token);
    const video = videoRef.current;

    if (Hls.isSupported()) {
      const hls = new Hls({ enableWorker: true, lowLatencyMode: true });
      hlsRef.current = hls;
      hls.loadSource(url);
      hls.attachMedia(video);
      hls.on(Hls.Events.MANIFEST_PARSED, () => {
        if (initialPositionSeconds > 0) video.currentTime = initialPositionSeconds;
        video.play().catch(() => setShowControls(true));
      });
      hls.on(Hls.Events.ERROR, (_, data) => {
        if (data.fatal) setError(data.details ?? "Stream error");
      });
    } else if (video.canPlayType("application/vnd.apple.mpegurl")) {
      video.src = url;
      if (initialPositionSeconds > 0) video.currentTime = initialPositionSeconds;
      video.play().catch(() => setShowControls(true));
    } else {
      setError("HLS playback unsupported in this browser.");
    }

    return () => {
      hlsRef.current?.destroy();
      hlsRef.current = null;
      if (token) api.delete(`/stream/${encodeURIComponent(token)}`).catch(() => {});
    };
  }, [token, initialPositionSeconds]);

  // Poll buffer
  useEffect(() => {
    if (!videoRef.current) return;
    const v = videoRef.current;
    const onPlay = () => setIsPlaying(true);
    const onPause = () => setIsPlaying(false);
    const onTime = () => {
      setPosition(v.currentTime);
      const buf = v.buffered;
      if (buf.length > 0) setBufferedEnd(buf.end(buf.length - 1));
      onProgress?.(v.currentTime, v.duration || 0);
    };
    const onMeta = () => setDuration(v.duration);
    const onEnd = () => onEnded?.();
    v.addEventListener("play", onPlay);
    v.addEventListener("pause", onPause);
    v.addEventListener("timeupdate", onTime);
    v.addEventListener("loadedmetadata", onMeta);
    v.addEventListener("ended", onEnd);
    return () => {
      v.removeEventListener("play", onPlay);
      v.removeEventListener("pause", onPause);
      v.removeEventListener("timeupdate", onTime);
      v.removeEventListener("loadedmetadata", onMeta);
      v.removeEventListener("ended", onEnd);
    };
  }, [onProgress, onEnded]);

  const togglePlay = useCallback(() => {
    const v = videoRef.current;
    if (!v) return;
    if (v.paused) v.play();
    else v.pause();
  }, []);

  const toggleMute = useCallback(() => {
    const v = videoRef.current;
    if (!v) return;
    v.muted = !v.muted;
    setMuted(v.muted);
  }, []);

  const seek = useCallback((seconds: number) => {
    const v = videoRef.current;
    if (!v) return;
    v.currentTime = Math.max(0, Math.min(v.duration || 0, seconds));
  }, []);

  const toggleFullscreen = useCallback(async () => {
    if (!containerRef.current) return;
    if (!document.fullscreenElement) {
      await containerRef.current.requestFullscreen();
      setIsFullscreen(true);
    } else {
      await document.exitFullscreen();
      setIsFullscreen(false);
    }
  }, []);

  const onPointerActivity = useCallback(() => {
    setShowControls(true);
    if (hideTimer.current) clearTimeout(hideTimer.current);
    hideTimer.current = setTimeout(() => setShowControls(false), 3000);
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === " ") { e.preventDefault(); togglePlay(); }
      if (e.key === "ArrowRight") seek(position + 10);
      if (e.key === "ArrowLeft") seek(position - 10);
      if (e.key === "m") toggleMute();
      if (e.key === "f") toggleFullscreen();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [togglePlay, toggleMute, toggleFullscreen, seek, position]);

  return (
    <div
      ref={containerRef}
      className="relative aspect-video w-full overflow-hidden rounded-3xl bg-black shadow-mythra-card"
      onMouseMove={onPointerActivity}
      onMouseLeave={() => setShowControls(false)}
      onClick={togglePlay}
    >
      <video
        ref={videoRef}
        playsInline
        className="h-full w-full"
        poster=""
      />

      {error && (
        <div className="absolute inset-0 grid place-items-center bg-black/70 text-rose-300 backdrop-blur">
          {error}
        </div>
      )}

      <AnimatePresence>
        {showControls && !error && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 20 }}
            transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
            className="pointer-events-auto absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/85 via-black/40 to-transparent px-6 pb-5 pt-16"
            onClick={(e) => e.stopPropagation()}
          >
            <SeekBar
              position={position}
              duration={duration}
              buffered={bufferedEnd}
              onSeek={seek}
            />
            <div className="mt-3 flex items-center gap-3 text-white">
              <ControlButton onClick={() => seek(position - 10)} label="Back 10s">
                <SkipBack size={18} />
              </ControlButton>
              <ControlButton onClick={togglePlay} label={isPlaying ? "Pause" : "Play"} highlighted>
                {isPlaying ? <Pause size={20} /> : <Play size={20} className="fill-current" />}
              </ControlButton>
              <ControlButton onClick={() => seek(position + 10)} label="Forward 10s">
                <SkipForward size={18} />
              </ControlButton>
              <div className="ml-2 flex items-center gap-2 text-xs tabular-nums text-white/80">
                <span>{formatTime(position)}</span>
                <span className="text-white/40">/</span>
                <span>{formatTime(duration)}</span>
              </div>
              <div className="ml-auto flex items-center gap-3">
                <ControlButton onClick={toggleMute} label={muted ? "Unmute" : "Mute"}>
                  {muted ? <VolumeX size={18} /> : <Volume2 size={18} />}
                </ControlButton>
                <input
                  type="range"
                  min={0}
                  max={1}
                  step={0.01}
                  value={muted ? 0 : volume}
                  onChange={(e) => {
                    const v = videoRef.current;
                    if (!v) return;
                    const val = Number(e.target.value);
                    v.volume = val;
                    setVolume(val);
                    if (val > 0 && muted) toggleMute();
                  }}
                  className="h-1 w-24 accent-mythra-purple"
                />
                <ControlButton onClick={toggleFullscreen} label="Fullscreen">
                  {isFullscreen ? <Minimize size={18} /> : <Maximize size={18} />}
                </ControlButton>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function SeekBar({
  position,
  duration,
  buffered,
  onSeek,
}: {
  position: number;
  duration: number;
  buffered: number;
  onSeek: (s: number) => void;
}) {
  const [hovering, setHovering] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const handleMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!ref.current || !duration) return;
    const rect = ref.current.getBoundingClientRect();
    const ratio = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    if (e.buttons === 1) onSeek(ratio * duration);
  };
  const handleClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!ref.current || !duration) return;
    const rect = ref.current.getBoundingClientRect();
    const ratio = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    onSeek(ratio * duration);
  };
  const pct = duration ? (position / duration) * 100 : 0;
  const buf = duration ? (buffered / duration) * 100 : 0;
  return (
    <div
      ref={ref}
      className={cn(
        "group relative h-2 cursor-pointer rounded-full bg-white/15",
        hovering && "h-2.5"
      )}
      onMouseEnter={() => setHovering(true)}
      onMouseLeave={() => setHovering(false)}
      onMouseMove={handleMove}
      onClick={handleClick}
    >
      <div className="absolute inset-y-0 left-0 rounded-full bg-white/25" style={{ width: `${buf}%` }} />
      <div
        className="absolute inset-y-0 left-0 rounded-full bg-gradient-to-r from-mythra-purple via-mythra-blue to-mythra-magenta"
        style={{ width: `${pct}%` }}
      />
      <div
        className={cn(
          "absolute top-1/2 h-3 w-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-white opacity-0 transition-opacity",
          hovering && "opacity-100"
        )}
        style={{ left: `${pct}%` }}
      />
    </div>
  );
}

function ControlButton({
  children,
  onClick,
  label,
  highlighted,
}: {
  children: React.ReactNode;
  onClick: () => void;
  label: string;
  highlighted?: boolean;
}) {
  return (
    <motion.button
      whileHover={{ scale: 1.06 }}
      whileTap={{ scale: 0.94 }}
      transition={{ duration: 0.15 }}
      onClick={onClick}
      aria-label={label}
      className={cn(
        "grid h-10 w-10 place-items-center rounded-full transition-colors",
        highlighted
          ? "bg-white text-black hover:bg-white/90"
          : "text-white/80 hover:bg-white/10 hover:text-white"
      )}
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
