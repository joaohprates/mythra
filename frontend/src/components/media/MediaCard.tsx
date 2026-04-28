"use client";

import { motion } from "framer-motion";
import { Play } from "lucide-react";
import Link from "next/link";
import { cardHover } from "@/lib/motion";
import type { MediaItem, SearchHit } from "@/lib/types";
import { cn } from "@/lib/cn";

type Item = MediaItem | SearchHit;

interface Props {
  item: Item;
  size?: "sm" | "md" | "lg";
  showOverview?: boolean;
}

export function MediaCard({ item, size = "md", showOverview = false }: Props) {
  const aspect = item.kind === "Video" || item.kind === "Audio" ? "aspect-[16/9]" : "aspect-[2/3]";
  const sizes = {
    sm: "min-w-[180px] w-[180px]",
    md: "min-w-[260px] w-[260px]",
    lg: "min-w-[340px] w-[340px]",
  };
  const poster = item.posterPath ?? null;
  const subtitle = "subtitle" in item ? item.subtitle : item.year ? String(item.year) : null;
  const overview = "overview" in item ? item.overview : null;

  return (
    <motion.div
      variants={cardHover}
      initial="rest"
      whileHover="hover"
      whileTap="tap"
      animate="rest"
      className={cn("relative gpu group", sizes[size])}
    >
      <Link href={hrefFor(item)} className="block">
        <div
          className={cn(
            "relative overflow-hidden rounded-2xl border border-white/[0.06]",
            aspect,
            "bg-gradient-to-br from-[#1a1d35] via-[#11132a] to-[#070811]"
          )}
        >
          {poster ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={poster}
              alt={item.title}
              loading="lazy"
              className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 ease-[cubic-bezier(0.16,1,0.3,1)] group-hover:scale-110"
            />
          ) : (
            <div className="absolute inset-0 grid place-items-center text-mythra-text-soft">
              <span className="text-xs uppercase tracking-widest">{item.kind}</span>
            </div>
          )}
          <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/85 via-black/15 to-transparent opacity-90 transition-opacity duration-300 group-hover:opacity-100" />
          <div className="absolute inset-x-0 bottom-0 p-4">
            <div className="flex items-end justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-white">{item.title}</p>
                {subtitle && <p className="truncate text-xs text-mythra-text-soft">{subtitle}</p>}
              </div>
              <motion.span
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.94 }}
                className="grid h-9 w-9 place-items-center rounded-full bg-white text-black shadow-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200"
              >
                <Play size={16} />
              </motion.span>
            </div>
            {showOverview && overview && (
              <p className="mt-2 line-clamp-2 text-xs text-mythra-text-soft">{overview}</p>
            )}
          </div>
          <KindBadge kind={item.kind} />
          {"rating" in item && item.rating ? (
            <span className="absolute right-2 top-2 rounded-full bg-black/60 backdrop-blur px-2 py-0.5 text-[11px] font-semibold text-amber-200">
              {item.rating.toFixed(1)}
            </span>
          ) : null}
        </div>
      </Link>
    </motion.div>
  );
}

function KindBadge({ kind }: { kind: string }) {
  const palette: Record<string, string> = {
    Video: "from-purple-500 to-blue-500",
    Manga: "from-rose-500 to-pink-600",
    Book: "from-amber-400 to-orange-500",
    Audio: "from-cyan-400 to-emerald-500",
  };
  return (
    <span
      className={cn(
        "absolute left-3 top-3 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-white shadow-md",
        "bg-gradient-to-r",
        palette[kind] ?? "from-zinc-600 to-zinc-500"
      )}
    >
      {kind}
    </span>
  );
}

function hrefFor(item: Item): string {
  return `/item/${item.id}`;
}
