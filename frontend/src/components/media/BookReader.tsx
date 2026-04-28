"use client";

import { motion, AnimatePresence } from "framer-motion";
import { Bookmark, ChevronLeft, ChevronRight, Sun, Type } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { cn } from "@/lib/cn";
import type { BookItemDetail } from "@/lib/types";

interface BookContent {
  chapterId: string;
  title: string;
  html: string;
}

interface Props {
  book: BookItemDetail;
  initialChapterId?: string;
}

export function BookReader({ book, initialChapterId }: Props) {
  const [chapterId, setChapterId] = useState(initialChapterId ?? book.chapters[0]?.id);
  const [theme, setTheme] = useState<"sepia" | "dark" | "midnight">("midnight");
  const [fontSize, setFontSize] = useState(18);
  const [chrome, setChrome] = useState(true);

  const content = useQuery({
    queryKey: ["book-chapter", book.id, chapterId],
    queryFn: async () => (await api.get<BookContent>(`/items/${book.id}/chapters/${chapterId}/content`)).data,
    enabled: !!chapterId,
  });

  const idx = useMemo(() => book.chapters.findIndex((c) => c.id === chapterId), [book.chapters, chapterId]);

  const palette = themes[theme];

  return (
    <div
      className="relative min-h-screen transition-colors duration-500"
      style={{ background: palette.bg, color: palette.fg }}
      onClick={() => setChrome((c) => !c)}
    >
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
              <h2 className="text-base font-semibold">{book.title}</h2>
              <select
                value={chapterId}
                onChange={(e) => setChapterId(e.target.value)}
                className="ml-auto rounded-full border border-white/10 bg-white/5 px-3 py-1.5 text-xs"
                style={{ color: palette.fg }}
              >
                {book.chapters.map((c) => (
                  <option key={c.id} value={c.id} className="bg-black">{c.title}</option>
                ))}
              </select>
            </div>
          </motion.header>
        )}
      </AnimatePresence>

      <article
        className="mx-auto max-w-3xl px-6 py-24 leading-loose tracking-wide"
        style={{ fontSize: `${fontSize}px`, fontFamily: "'Plus Jakarta Sans', serif" }}
      >
        {content.isLoading && <p className="text-center opacity-60">Loading chapter…</p>}
        {content.isError && (
          <p className="opacity-70">
            Reader content endpoint not yet implemented for this format. Chapter metadata is available, but the body
            stream needs <code>/items/{book.id}/chapters/{chapterId}/content</code>.
          </p>
        )}
        {content.data && (
          <motion.div
            key={chapterId}
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.16, 1, 0.3, 1] }}
            dangerouslySetInnerHTML={{ __html: content.data.html }}
          />
        )}
      </article>

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
              <NavButton onClick={() => idx > 0 && setChapterId(book.chapters[idx - 1].id)} disabled={idx <= 0}>
                <ChevronLeft size={16} />
              </NavButton>
              <span className="text-xs opacity-70">
                {idx + 1} / {book.chapters.length}
              </span>
              <NavButton
                onClick={() => idx < book.chapters.length - 1 && setChapterId(book.chapters[idx + 1].id)}
                disabled={idx >= book.chapters.length - 1}
              >
                <ChevronRight size={16} />
              </NavButton>
              <div className="ml-auto flex items-center gap-2">
                <button
                  onClick={() => setFontSize((s) => Math.max(14, s - 1))}
                  className="grid h-9 w-9 place-items-center rounded-full hover:bg-white/10"
                >
                  <Type size={14} />−
                </button>
                <button
                  onClick={() => setFontSize((s) => Math.min(28, s + 1))}
                  className="grid h-9 w-9 place-items-center rounded-full hover:bg-white/10"
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
                <button className="grid h-9 w-9 place-items-center rounded-full hover:bg-white/10">
                  <Bookmark size={14} />
                </button>
              </div>
            </div>
          </motion.footer>
        )}
      </AnimatePresence>
    </div>
  );
}

function NavButton({ onClick, disabled, children }: { onClick: () => void; disabled?: boolean; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={cn("grid h-9 w-9 place-items-center rounded-full hover:bg-white/10", disabled && "cursor-not-allowed opacity-40")}
    >
      {children}
    </button>
  );
}

const themes = {
  sepia: { bg: "linear-gradient(180deg, #f6efe2 0%, #ebe2d2 100%)", fg: "#3a2c1a", chrome: "rgba(255,255,255,0.55)" },
  dark: { bg: "#0c0e1a", fg: "#e6e1ff", chrome: "rgba(0,0,0,0.55)" },
  midnight: {
    bg: "radial-gradient(ellipse at top, rgba(168,85,247,0.18), transparent 50%), #06070d",
    fg: "#f1efff",
    chrome: "rgba(0,0,0,0.65)",
  },
};

function rotateTheme(t: keyof typeof themes): keyof typeof themes {
  const order: Array<keyof typeof themes> = ["midnight", "dark", "sepia"];
  return order[(order.indexOf(t) + 1) % order.length];
}
