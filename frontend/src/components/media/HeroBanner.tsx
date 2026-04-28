"use client";

import { motion, AnimatePresence } from "framer-motion";
import { Info, Play } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { heroBackdrop, overlayGradient } from "@/lib/motion";
import type { MediaItem } from "@/lib/types";

interface Props {
  items: MediaItem[];
  intervalMs?: number;
}

export function HeroBanner({ items, intervalMs = 8000 }: Props) {
  const [index, setIndex] = useState(0);

  useEffect(() => {
    if (items.length <= 1) return;
    const id = setInterval(() => setIndex((i) => (i + 1) % items.length), intervalMs);
    return () => clearInterval(id);
  }, [items.length, intervalMs]);

  if (items.length === 0) return null;
  const current = items[index];

  return (
    <section className="relative -mx-6 mb-10 h-[72vh] min-h-[540px] overflow-hidden lg:-mx-10">
      <AnimatePresence mode="popLayout">
        <motion.div
          key={current.id}
          variants={heroBackdrop}
          initial="hidden"
          animate="visible"
          exit="exit"
          className="absolute inset-0"
        >
          {current.backdropPath ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={current.backdropPath}
              alt=""
              className="h-full w-full object-cover"
              loading="eager"
            />
          ) : (
            <div className="h-full w-full bg-gradient-to-br from-[#1a1d35] via-[#11132a] to-[#070811]" />
          )}
        </motion.div>
      </AnimatePresence>

      <div className="absolute inset-0" style={{ backgroundImage: overlayGradient.scrim }} />
      <div className="absolute inset-0" style={{ backgroundImage: overlayGradient.side }} />

      <div className="relative z-10 mx-auto flex h-full max-w-[1700px] flex-col justify-end px-6 pb-16 lg:px-10">
        <AnimatePresence mode="popLayout">
          <motion.div
            key={current.id}
            initial={{ opacity: 0, y: 32, filter: "blur(8px)" }}
            animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
            exit={{ opacity: 0, y: -16, filter: "blur(6px)" }}
            transition={{ duration: 0.7, ease: [0.16, 1, 0.3, 1] }}
            className="max-w-2xl"
          >
            <span className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-black/30 backdrop-blur px-3 py-1 text-[11px] font-semibold uppercase tracking-widest text-mythra-text-soft">
              <span className="h-1.5 w-1.5 rounded-full bg-mythra-purple shadow-[0_0_12px_rgba(168,85,247,0.9)]" />
              {current.kind === "Video" ? "Featured" : current.kind}
            </span>
            <h1 className="mt-4 text-4xl font-bold tracking-tight md:text-6xl lg:text-7xl">
              <span className="gradient-text">{current.title}</span>
            </h1>
            {current.tagline && (
              <p className="mt-3 text-base text-mythra-text-muted md:text-lg">{current.tagline}</p>
            )}
            {current.overview && (
              <p className="mt-5 line-clamp-3 max-w-xl text-sm leading-relaxed text-mythra-text-muted md:text-base">
                {current.overview}
              </p>
            )}

            <div className="mt-7 flex flex-wrap items-center gap-3">
              <Link
                href={primaryHref(current)}
                className="group inline-flex items-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold text-black shadow-[0_18px_50px_-15px_rgba(255,255,255,0.5)] transition hover:scale-[1.03] hover:shadow-[0_22px_55px_-15px_rgba(255,255,255,0.7)]"
              >
                <Play size={16} className="fill-current" /> Play now
              </Link>
              <Link
                href={`/item/${current.id}`}
                className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.06] px-6 py-3 text-sm font-semibold text-white backdrop-blur transition hover:bg-white/[0.12]"
              >
                <Info size={16} /> More info
              </Link>
            </div>
          </motion.div>
        </AnimatePresence>

        {items.length > 1 && (
          <div className="mt-10 flex items-center gap-2">
            {items.map((it, i) => (
              <button
                key={it.id}
                onClick={() => setIndex(i)}
                aria-label={`Show ${it.title}`}
                className="group relative h-1 w-12 overflow-hidden rounded-full bg-white/15"
              >
                {i === index && (
                  <motion.span
                    layoutId="hero-progress"
                    className="absolute inset-y-0 left-0 w-full origin-left bg-gradient-to-r from-mythra-purple via-mythra-blue to-mythra-magenta"
                    initial={{ scaleX: 0 }}
                    animate={{ scaleX: 1 }}
                    transition={{ duration: intervalMs / 1000, ease: "linear" }}
                  />
                )}
              </button>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

function primaryHref(item: MediaItem): string {
  switch (item.kind) {
    case "Video":
      return `/watch/${item.id}`;
    case "Manga":
    case "Book":
      return `/read/${item.id}`;
    case "Audio":
      return `/listen/${item.id}`;
    default:
      return `/item/${item.id}`;
  }
}
