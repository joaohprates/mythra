"use client";

import { motion, AnimatePresence } from "framer-motion";
import { Bookmark, ChevronLeft, ChevronRight, Download, Sun, Type } from "lucide-react";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { cn } from "@/lib/cn";
import type { BookItemDetail } from "@/lib/types";

interface BookContent {
  chapterId: string;
  title: string;
  html: string;
  unsupported?: boolean;
  format?: string;
  downloadUrl?: string;
}

interface Props {
  book: BookItemDetail;
  initialChapterId?: string;
}

export function BookReader({ book, initialChapterId }: Props) {
  if (book.format === "Pdf") return <PdfReader book={book} />;
  if (book.format === "Mobi" || book.format === "Azw3") return <UnsupportedFormatReader book={book} />;
  return <EpubReader book={book} initialChapterId={initialChapterId} />;
}

// ── PDF ──────────────────────────────────────────────────────────────────────

function PdfReader({ book }: { book: BookItemDetail }) {
  const fileUrl = `/api/v1/download/${book.id}`;
  return (
    <div className="flex h-screen flex-col bg-[#06070d]">
      <div className="flex items-center gap-3 border-b border-white/[0.06] px-6 py-3">
        <h2 className="text-sm font-semibold">{book.title}</h2>
        {book.author && <span className="text-xs text-mythra-text-muted">by {book.author}</span>}
        <a
          href={fileUrl}
          download
          className="ml-auto inline-flex items-center gap-1.5 rounded-full border border-white/10 px-3 py-1.5 text-xs hover:bg-white/10"
        >
          <Download size={12} /> Download PDF
        </a>
      </div>
      <object data={fileUrl} type="application/pdf" className="flex-1 w-full">
        <div className="flex h-full flex-col items-center justify-center gap-4 text-center px-6">
          <p className="text-sm text-mythra-text-muted">Your browser cannot display PDFs inline.</p>
          <a
            href={fileUrl}
            download
            className="rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2.5 text-sm font-medium text-white"
          >
            Download PDF
          </a>
        </div>
      </object>
    </div>
  );
}

// ── MOBI / AZW3 ──────────────────────────────────────────────────────────────

function UnsupportedFormatReader({ book }: { book: BookItemDetail }) {
  const fileUrl = `/api/v1/download/${book.id}`;
  return (
    <div className="flex h-screen flex-col items-center justify-center gap-6 bg-[#06070d] text-center px-6">
      <div className="rounded-3xl border border-white/[0.06] bg-white/[0.02] p-10 max-w-md">
        <p className="text-lg font-semibold">{book.title}</p>
        {book.author && <p className="mt-1 text-sm text-mythra-text-muted">by {book.author}</p>}
        <div className="mt-4 rounded-2xl border border-amber-400/20 bg-amber-400/10 px-4 py-3 text-xs text-amber-300">
          <span className="font-semibold uppercase">{book.format}</span> files cannot be rendered
          inline. Download and open in Calibre, Kindle, or your e-reader app.
        </div>
        <a
          href={fileUrl}
          download
          className="mt-5 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-6 py-2.5 text-sm font-medium text-white"
        >
          <Download size={14} /> Download {book.format}
        </a>
      </div>
    </div>
  );
}

// ── EPUB reader ──────────────────────────────────────────────────────────────

function EpubReader({ book, initialChapterId }: Props) {
  const hasNavChapters = book.chapters.length > 0;

  const [chapterId, setChapterId] = useState(initialChapterId ?? book.chapters[0]?.id ?? "");
  const [roIndex, setRoIndex] = useState(0);
  const [theme, setTheme] = useState<"sepia" | "dark" | "midnight">("midnight");
  const [fontSize, setFontSize] = useState(18);
  const [chrome, setChrome] = useState(true);

  // When the EPUB has no TOC, fetch the reading order from the server
  const readingOrder = useQuery({
    queryKey: ["reading-order", book.id],
    queryFn: async () =>
      (await api.get<{ id: number; title: string }[]>(`/items/${book.id}/reading-order`)).data,
    enabled: !hasNavChapters,
  });

  // Display chapter list
  const displayChapters = hasNavChapters
    ? book.chapters.map((c) => ({ id: c.id, title: c.title }))
    : (readingOrder.data ?? []).map((r) => ({ id: String(r.id), title: r.title }));

  const currentIndex = hasNavChapters
    ? displayChapters.findIndex((c) => c.id === chapterId)
    : roIndex;

  const totalChapters = displayChapters.length;
  const canPrev = currentIndex > 0;
  const canNext = currentIndex < totalChapters - 1;

  function goPrev() {
    if (!canPrev) return;
    if (hasNavChapters) setChapterId(displayChapters[currentIndex - 1].id);
    else setRoIndex(roIndex - 1);
  }
  function goNext() {
    if (!canNext) return;
    if (hasNavChapters) setChapterId(displayChapters[currentIndex + 1].id);
    else setRoIndex(roIndex + 1);
  }
  function selectChapter(id: string) {
    if (hasNavChapters) setChapterId(id);
    else setRoIndex(Number(id));
  }

  // Fetch chapter content
  const contentKey = hasNavChapters ? chapterId : roIndex;
  const content = useQuery({
    queryKey: ["book-chapter", book.id, contentKey],
    queryFn: async () => {
      if (hasNavChapters) {
        return (await api.get<BookContent>(`/items/${book.id}/chapters/${chapterId}/content`)).data;
      }
      // Reading-order fallback: no per-index endpoint yet → show placeholder
      const items = readingOrder.data ?? [];
      const item = items[roIndex];
      return {
        chapterId: String(roIndex),
        title: item?.title ?? `Part ${roIndex + 1}`,
        html: "<p style='opacity:0.6;text-align:center'>Chapter content unavailable — this EPUB has no navigation data.</p>",
      } satisfies BookContent;
    },
    enabled: hasNavChapters ? !!chapterId : readingOrder.isSuccess,
  });

  const palette = themes[theme];

  return (
    <div
      className="relative min-h-screen transition-colors duration-500"
      style={{ background: palette.bg, color: palette.fg }}
      onClick={() => setChrome((c) => !c)}
    >
      {/* ── Header chrome ── */}
      <AnimatePresence>
        {chrome && (
          <motion.header
            initial={{ opacity: 0, y: -16 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -16 }}
            className="absolute inset-x-0 top-0 z-20 px-6 py-4"
            onClick={(e) => e.stopPropagation()}
            style={{ background: palette.chrome }}
          >
            <div className="mx-auto flex max-w-3xl items-center gap-3">
              <h2 className="truncate text-base font-semibold">{book.title}</h2>
              {displayChapters.length > 1 && (
                <select
                  value={hasNavChapters ? chapterId : String(roIndex)}
                  onChange={(e) => selectChapter(e.target.value)}
                  className="ml-auto rounded-full border border-white/10 bg-white/5 px-3 py-1.5 text-xs"
                  style={{ color: palette.fg }}
                >
                  {displayChapters.map((c) => (
                    <option key={c.id} value={c.id} className="bg-black">
                      {c.title}
                    </option>
                  ))}
                </select>
              )}
            </div>
          </motion.header>
        )}
      </AnimatePresence>

      {/* ── Article ── */}
      <article
        className="mx-auto max-w-3xl px-6 py-24 leading-loose tracking-wide"
        style={{ fontSize: `${fontSize}px`, fontFamily: "'Plus Jakarta Sans', Georgia, serif" }}
      >
        {content.isLoading && (
          <div className="space-y-3 opacity-30 animate-pulse">
            {Array.from({ length: 10 }).map((_, i) => (
              <div
                key={i}
                className="h-4 rounded-full bg-current"
                style={{ width: `${55 + (i * 7) % 45}%` }}
              />
            ))}
          </div>
        )}

        {content.isError && (
          <p className="opacity-70 text-sm text-center py-12">
            Failed to load chapter. The EPUB file may be password-protected or corrupted.
          </p>
        )}

        {content.data?.unsupported && (
          <div className="flex flex-col items-center gap-4 py-12 text-center">
            <p className="text-sm opacity-70">This file cannot be rendered inline.</p>
            <a
              href={content.data.downloadUrl ?? `/api/v1/download/${book.id}`}
              download
              className="inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2.5 text-sm font-medium text-white"
            >
              <Download size={14} /> Download to read
            </a>
          </div>
        )}

        {content.data && !content.isLoading && !content.data.unsupported && (
          <motion.div
            key={String(contentKey)}
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.16, 1, 0.3, 1] }}
            className="epub-body"
            dangerouslySetInnerHTML={{ __html: content.data.html }}
          />
        )}
      </article>

      {/* ── Footer chrome ── */}
      <AnimatePresence>
        {chrome && (
          <motion.footer
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 16 }}
            className="absolute inset-x-0 bottom-0 z-20 px-6 py-3"
            onClick={(e) => e.stopPropagation()}
            style={{ background: palette.chrome }}
          >
            <div className="mx-auto flex max-w-3xl items-center gap-2">
              <NavButton onClick={goPrev} disabled={!canPrev}>
                <ChevronLeft size={16} />
              </NavButton>
              <span className="text-xs opacity-70">
                {currentIndex + 1} / {totalChapters || "?"}
              </span>
              <NavButton onClick={goNext} disabled={!canNext}>
                <ChevronRight size={16} />
              </NavButton>

              <div className="ml-auto flex items-center gap-2">
                <button
                  onClick={() => setFontSize((s) => Math.max(14, s - 1))}
                  className="grid h-9 w-9 place-items-center rounded-full text-xs hover:bg-white/10"
                  title="Smaller text"
                >
                  <Type size={12} />−
                </button>
                <button
                  onClick={() => setFontSize((s) => Math.min(28, s + 1))}
                  className="grid h-9 w-9 place-items-center rounded-full text-xs hover:bg-white/10"
                  title="Larger text"
                >
                  <Type size={14} />+
                </button>
                <button
                  onClick={() => setTheme(rotateTheme(theme))}
                  className="grid h-9 w-9 place-items-center rounded-full hover:bg-white/10"
                  aria-label="Cycle theme"
                >
                  <Sun size={14} />
                </button>
                <a
                  href={`/api/v1/download/${book.id}`}
                  download
                  onClick={(e) => e.stopPropagation()}
                  className="grid h-9 w-9 place-items-center rounded-full hover:bg-white/10"
                  title="Download"
                >
                  <Download size={14} />
                </a>
                <button className="grid h-9 w-9 place-items-center rounded-full hover:bg-white/10">
                  <Bookmark size={14} />
                </button>
              </div>
            </div>
          </motion.footer>
        )}
      </AnimatePresence>

      {/* EPUB typography reset */}
      <style>{`
        .epub-body { word-break: break-word; }
        .epub-body p  { margin-bottom: 1em; }
        .epub-body h1 { font-size: 1.6em; font-weight: 700; margin: 1.5em 0 0.5em; line-height: 1.25; }
        .epub-body h2 { font-size: 1.3em; font-weight: 700; margin: 1.4em 0 0.4em; }
        .epub-body h3 { font-size: 1.1em; font-weight: 600; margin: 1.2em 0 0.3em; }
        .epub-body blockquote { border-left: 3px solid currentColor; padding-left: 1em; opacity: 0.75; margin: 1em 0; font-style: italic; }
        .epub-body img { max-width: 100%; height: auto; border-radius: 8px; margin: 1.2em auto; display: block; }
        .epub-body a { text-decoration: none; pointer-events: none; }
        .epub-body table { border-collapse: collapse; width: 100%; margin: 1em 0; font-size: 0.9em; }
        .epub-body td, .epub-body th { padding: 0.4em 0.6em; border: 1px solid rgba(128,128,128,0.3); }
        .epub-body em, .epub-body i { font-style: italic; }
        .epub-body strong, .epub-body b { font-weight: 700; }
        .epub-body hr { border: none; border-top: 1px solid rgba(128,128,128,0.25); margin: 2em 0; }
        .epub-body pre, .epub-body code { font-family: monospace; font-size: 0.9em; }
        .epub-body ul, .epub-body ol { padding-left: 1.5em; margin-bottom: 1em; }
        .epub-body li { margin-bottom: 0.3em; }
      `}</style>
    </div>
  );
}

// ── Shared ────────────────────────────────────────────────────────────────────

function NavButton({
  onClick,
  disabled,
  children,
}: {
  onClick: () => void;
  disabled?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={cn(
        "grid h-9 w-9 place-items-center rounded-full hover:bg-white/10",
        disabled && "cursor-not-allowed opacity-40"
      )}
    >
      {children}
    </button>
  );
}

const themes = {
  sepia: { bg: "linear-gradient(180deg,#f6efe2 0%,#ebe2d2 100%)", fg: "#3a2c1a", chrome: "rgba(255,255,255,0.55)" },
  dark: { bg: "#0c0e1a", fg: "#e6e1ff", chrome: "rgba(0,0,0,0.55)" },
  midnight: {
    bg: "radial-gradient(ellipse at top,rgba(168,85,247,0.18),transparent 50%),#06070d",
    fg: "#f1efff",
    chrome: "rgba(0,0,0,0.65)",
  },
};

function rotateTheme(t: keyof typeof themes): keyof typeof themes {
  const order: Array<keyof typeof themes> = ["midnight", "dark", "sepia"];
  return order[(order.indexOf(t) + 1) % order.length];
}
