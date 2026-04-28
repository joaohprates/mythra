"use client";

import { motion } from "framer-motion";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useCallback, useRef } from "react";
import { stagger } from "@/lib/motion";
import type { MediaItem, SearchHit } from "@/lib/types";
import { MediaCard } from "./MediaCard";

interface Props {
  title: string;
  subtitle?: string;
  items: Array<MediaItem | SearchHit>;
  size?: "sm" | "md" | "lg";
  loading?: boolean;
}

export function ContentRow({ title, subtitle, items, size = "md", loading }: Props) {
  const ref = useRef<HTMLDivElement>(null);
  const scroll = useCallback((direction: 1 | -1) => {
    if (!ref.current) return;
    const amount = ref.current.clientWidth * 0.85 * direction;
    ref.current.scrollBy({ left: amount, behavior: "smooth" });
  }, []);

  return (
    <section className="relative">
      <div className="mb-4 flex items-end justify-between gap-4 px-1">
        <div>
          <h2 className="text-xl font-semibold tracking-tight text-white md:text-2xl">{title}</h2>
          {subtitle && <p className="mt-1 text-sm text-mythra-text-soft">{subtitle}</p>}
        </div>
        <div className="hidden items-center gap-2 md:flex">
          <button
            onClick={() => scroll(-1)}
            className="grid h-10 w-10 place-items-center rounded-full border border-white/[0.06] bg-white/[0.03] text-mythra-text-muted transition hover:bg-white/10 hover:text-white"
            aria-label="Scroll left"
          >
            <ChevronLeft size={18} />
          </button>
          <button
            onClick={() => scroll(1)}
            className="grid h-10 w-10 place-items-center rounded-full border border-white/[0.06] bg-white/[0.03] text-mythra-text-muted transition hover:bg-white/10 hover:text-white"
            aria-label="Scroll right"
          >
            <ChevronRight size={18} />
          </button>
        </div>
      </div>

      <motion.div
        ref={ref}
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true, margin: "-10% 0px" }}
        variants={stagger(0.04)}
        className="no-scrollbar -mx-2 flex gap-4 overflow-x-auto px-2 pb-4 snap-x snap-mandatory"
      >
        {loading
          ? Array.from({ length: 8 }).map((_, i) => <SkeletonCard key={i} size={size} />)
          : items.map((item) => (
              <motion.div
                key={item.id}
                variants={{ hidden: { opacity: 0, y: 16 }, visible: { opacity: 1, y: 0 } }}
                className="snap-start"
              >
                <MediaCard item={item} size={size} />
              </motion.div>
            ))}
      </motion.div>
    </section>
  );
}

function SkeletonCard({ size }: { size: "sm" | "md" | "lg" }) {
  const sizes = { sm: "w-[180px]", md: "w-[260px]", lg: "w-[340px]" };
  return (
    <div className={`${sizes[size]} aspect-[2/3] rounded-2xl border border-white/[0.04] bg-white/[0.03] shimmer`} />
  );
}
