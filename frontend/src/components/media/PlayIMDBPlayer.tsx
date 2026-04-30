"use client";

import { motion } from "framer-motion";
import { ExternalLink } from "lucide-react";

interface Props {
  imdbId: string;
  title: string;
}

/**
 * Embeds PlayIMDB (https://www.playimdb.com) in a sandboxed iframe.
 * PlayIMDB uses the same title IDs as IMDB (e.g. "tt1234567").
 */
export function PlayIMDBPlayer({ imdbId, title }: Props) {
  const src = `https://www.playimdb.com/pt/title/${imdbId}/`;

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.98 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
      className="relative aspect-video w-full overflow-hidden rounded-3xl bg-black shadow-mythra-card"
    >
      <iframe
        src={src}
        title={title}
        allowFullScreen
        referrerPolicy="no-referrer"
        allow="autoplay; fullscreen; picture-in-picture"
        className="h-full w-full border-0"
        sandbox="allow-scripts allow-same-origin allow-forms allow-popups allow-presentation allow-pointer-lock allow-top-navigation"
      />

      {/* Provider badge */}
      <div className="pointer-events-none absolute bottom-3 right-3 flex items-center gap-1.5 rounded-full bg-black/60 px-3 py-1.5 text-[11px] text-white/60 backdrop-blur">
        <ExternalLink size={11} />
        PlayIMDB
      </div>
    </motion.div>
  );
}
