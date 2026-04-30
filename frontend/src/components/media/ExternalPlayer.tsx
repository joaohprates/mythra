"use client";

import { motion } from "framer-motion";
import { ExternalLink, Loader2, AlertTriangle } from "lucide-react";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";

interface ExternalStreamDto {
  providerName: string;
  streamKind: "IframeEmbed" | "HlsManifest" | "DirectMp4";
  url: string;
  refererUrl?: string | null;
  headers?: Record<string, string> | null;
}

interface Props {
  mediaItemId: string;
  season?: number | null;
  episode?: number | null;
  title: string;
}

/**
 * Fetches an external stream URL and renders the appropriate player:
 * - IframeEmbed  → sandboxed <iframe> (Vidsrc, etc.)
 * - HlsManifest  → redirects to the HLS URL (browser-native or hls.js)
 * - DirectMp4    → <video> element
 */
export function ExternalPlayer({ mediaItemId, season, episode, title }: Props) {
  const [stream, setStream] = useState<ExternalStreamDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    const params: Record<string, string | number> = {};
    if (season != null) params.season = season;
    if (episode != null) params.episode = episode;

    api
      .get<ExternalStreamDto>(`/stream/external/${mediaItemId}`, { params })
      .then((r) => setStream(r.data))
      .catch(() => setError("No external stream available for this title right now."))
      .finally(() => setLoading(false));
  }, [mediaItemId, season, episode]);

  if (loading) {
    return (
      <div className="flex aspect-video w-full items-center justify-center rounded-3xl bg-black/60">
        <Loader2 size={32} className="animate-spin text-mythra-purple" />
      </div>
    );
  }

  if (error || !stream) {
    return (
      <div className="flex aspect-video w-full flex-col items-center justify-center gap-3 rounded-3xl bg-black/60 text-mythra-text-muted">
        <AlertTriangle size={32} className="text-amber-400" />
        <p className="text-sm">{error ?? "Stream unavailable."}</p>
      </div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.98 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
      className="relative aspect-video w-full overflow-hidden rounded-3xl bg-black shadow-mythra-card"
    >
      {stream.streamKind === "IframeEmbed" && (
        <>
          <iframe
            src={stream.url}
            title={title}
            allowFullScreen
            referrerPolicy="origin"
            className="h-full w-full border-0"
            sandbox="allow-scripts allow-same-origin allow-forms allow-popups allow-presentation allow-pointer-lock"
          />
          {/* Provider badge */}
          <div className="absolute bottom-3 right-3 flex items-center gap-1.5 rounded-full bg-black/60 px-3 py-1.5 text-[11px] text-white/60 backdrop-blur">
            <ExternalLink size={11} />
            {stream.providerName}
          </div>
        </>
      )}

      {stream.streamKind === "DirectMp4" && (
        // eslint-disable-next-line jsx-a11y/media-has-caption
        <video
          src={stream.url}
          controls
          autoPlay
          playsInline
          className="h-full w-full"
        />
      )}

      {stream.streamKind === "HlsManifest" && (
        // For HLS manifest from external source, open natively
        // (browsers that support HLS natively, like Safari, handle this)
        // eslint-disable-next-line jsx-a11y/media-has-caption
        <video
          src={stream.url}
          controls
          autoPlay
          playsInline
          className="h-full w-full"
        />
      )}
    </motion.div>
  );
}
