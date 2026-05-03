"use client";

import { motion, AnimatePresence } from "framer-motion";
import { ChevronLeft, ChevronRight, ExternalLink, Settings } from "lucide-react";
import { useEffect, useState } from "react";
import { cn } from "@/lib/cn";
import type { MangaItemDetail, MangaReadingDirection } from "@/lib/types";
import { useTranslation } from "@/store/locale";

interface Props {
  manga: MangaItemDetail;
  initialChapterId?: string;
}

export function MangaReader({ manga, initialChapterId }: Props) {
  const t = useTranslation();
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

  // ── External-only: no local chapters ──────────────────────────────────────
  if (manga.isExternal || manga.chapters.length === 0) {
    return (
      <div className="grid min-h-screen place-items-center bg-black px-6">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex max-w-sm flex-col items-center gap-6 text-center"
        >
          {manga.posterPath && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={manga.posterPath}
              alt={manga.title}
              className="h-52 w-auto rounded-2xl shadow-2xl"
            />
          )}
          <div>
            <h2 className="text-xl font-bold text-white">{manga.title}</h2>
            <p className="mt-2 text-sm text-white/50">
              {t("reader.external.message")}
            </p>
          </div>
          <div className="flex w-full flex-col gap-3">
            {manga.mangaDexUrl && (
              <a
                href={manga.mangaDexUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center gap-2 rounded-2xl border border-orange-400/30 bg-orange-500/10 px-5 py-3 text-sm font-semibold text-orange-300 transition hover:bg-orange-500/20"
              >
                <ExternalLink size={15} />
                {t("reader.readOnMangaDex")}
              </a>
            )}
            {manga.anilistUrl && (
              <a
                href={manga.anilistUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center gap-2 rounded-2xl border border-blue-400/30 bg-blue-500/10 px-5 py-3 text-sm font-semibold text-blue-300 transition hover:bg-blue-500/20"
              >
                <ExternalLink size={15} />
                {t("reader.viewOnAniList")}
              </a>
            )}
            {!manga.mangaDexUrl && !manga.anilistUrl && (
              <a
                href={`https://mangadex.org/search?q=${encodeURIComponent(manga.title)}`}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center gap-2 rounded-2xl border border-white/10 bg-white/[0.04] px-5 py-3 text-sm font-semibold text-white/70 transition hover:bg-white/[0.08]"
              >
                <ExternalLink size={15} />
                {t("reader.searchOnMangaDex")}
              </a>
            )}
          </div>
        </motion.div>
      </div>
    );
  }

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
