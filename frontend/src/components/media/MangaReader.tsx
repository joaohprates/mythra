"use client";

import { motion, AnimatePresence } from "framer-motion";
import { ChevronLeft, ChevronRight, Layers, Settings } from "lucide-react";
import { useEffect, useState } from "react";
import { cn } from "@/lib/cn";
import type { MangaItemDetail, MangaReadingDirection } from "@/lib/types";

interface Props {
  manga: MangaItemDetail;
  initialChapterId?: string;
}

export function MangaReader({ manga, initialChapterId }: Props) {
  const [direction, setDirection] = useState<MangaReadingDirection>(manga.readingDirection ?? "RightToLeft");
  const [chapterId, setChapterId] = useState(initialChapterId ?? manga.chapters[0]?.id);
  const chapter = manga.chapters.find((c) => c.id === chapterId) ?? manga.chapters[0];
  const [pageIndex, setPageIndex] = useState(0);
  const [chrome, setChrome] = useState(true);

  useEffect(() => {
    setPageIndex(0);
  }, [chapterId]);

  const next = () => setPageIndex((p) => Math.min((chapter?.pageCount ?? 1) - 1, p + 1));
  const prev = () => setPageIndex((p) => Math.max(0, p - 1));

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "ArrowRight") direction === "RightToLeft" ? prev() : next();
      if (e.key === "ArrowLeft") direction === "RightToLeft" ? next() : prev();
      if (e.key === " ") { e.preventDefault(); next(); }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [chapter, direction]);

  if (!chapter) return <div className="grid min-h-screen place-items-center text-mythra-text-soft">No chapters yet.</div>;

  const pageUrl = `/api/v1/items/${manga.id}/chapters/${chapter.id}/pages/${pageIndex}`;

  return (
    <div className="relative min-h-screen bg-black" onClick={() => setChrome((c) => !c)}>
      <AnimatePresence>
        {chrome && (
          <motion.header
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="absolute inset-x-0 top-0 z-30 bg-gradient-to-b from-black/85 to-transparent px-6 py-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mx-auto flex max-w-[1700px] items-center gap-4">
              <h2 className="text-lg font-semibold text-white">{manga.title}</h2>
              <select
                value={chapter.id}
                onChange={(e) => setChapterId(e.target.value)}
                className="rounded-full border border-white/10 bg-white/[0.05] px-3 py-1.5 text-sm text-white"
              >
                {manga.chapters.map((c) => (
                  <option key={c.id} value={c.id} className="bg-black">
                    Ch. {c.chapterNumber} {c.title ? `• ${c.title}` : ""}
                  </option>
                ))}
              </select>
              <div className="ml-auto flex items-center gap-2 text-xs text-white/80">
                <button
                  onClick={() => setDirection(direction === "RightToLeft" ? "LeftToRight" : "RightToLeft")}
                  className="rounded-full border border-white/10 px-3 py-1 hover:bg-white/10"
                >
                  {direction === "RightToLeft" ? "RTL" : "LTR"}
                </button>
                <span className="rounded-full border border-white/10 px-3 py-1">
                  {pageIndex + 1} / {chapter.pageCount}
                </span>
              </div>
            </div>
          </motion.header>
        )}
      </AnimatePresence>

      <div className="flex min-h-screen items-center justify-center">
        <motion.img
          key={pageUrl}
          src={pageUrl}
          alt={`Page ${pageIndex + 1}`}
          initial={{ opacity: 0, scale: 1.01 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.35, ease: [0.16, 1, 0.3, 1] }}
          className={cn("max-h-screen w-auto", direction === "RightToLeft" ? "" : "")}
        />
      </div>

      <button
        onClick={(e) => { e.stopPropagation(); prev(); }}
        className="absolute left-0 top-0 z-20 h-full w-1/4 cursor-w-resize"
        aria-label="Previous page"
      />
      <button
        onClick={(e) => { e.stopPropagation(); next(); }}
        className="absolute right-0 top-0 z-20 h-full w-1/4 cursor-e-resize"
        aria-label="Next page"
      />

      <AnimatePresence>
        {chrome && (
          <motion.footer
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 20 }}
            className="absolute inset-x-0 bottom-0 z-30 bg-gradient-to-t from-black/85 to-transparent px-6 py-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mx-auto flex max-w-[1700px] items-center gap-3 text-white">
              <button onClick={prev} className="grid h-10 w-10 place-items-center rounded-full bg-white/5 hover:bg-white/15">
                <ChevronLeft size={18} />
              </button>
              <button onClick={next} className="grid h-10 w-10 place-items-center rounded-full bg-white/5 hover:bg-white/15">
                <ChevronRight size={18} />
              </button>
              <div className="ml-2 flex-1">
                <input
                  type="range"
                  min={0}
                  max={chapter.pageCount - 1}
                  value={pageIndex}
                  onChange={(e) => setPageIndex(parseInt(e.target.value, 10))}
                  className="w-full accent-mythra-purple"
                />
              </div>
              <button className="grid h-10 w-10 place-items-center rounded-full bg-white/5 hover:bg-white/15">
                <Settings size={16} />
              </button>
            </div>
          </motion.footer>
        )}
      </AnimatePresence>
    </div>
  );
}
