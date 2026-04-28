"use client";

import { useQuery } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { Search } from "lucide-react";
import { useEffect, useState } from "react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { MediaCard } from "@/components/media/MediaCard";
import { api } from "@/lib/api";
import type { MediaKind, SearchResult } from "@/lib/types";

const KINDS: MediaKind[] = ["Video", "Manga", "Book", "Audio"];

export default function SearchPage() {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [filterKinds, setFilterKinds] = useState<MediaKind[]>([]);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(query), 220);
    return () => clearTimeout(t);
  }, [query]);

  const search = useQuery({
    queryKey: ["search", debounced, filterKinds],
    queryFn: async () => {
      if (!debounced) return { hits: [], total: 0, elapsedMs: 0 } as SearchResult;
      const res = await api.post<SearchResult>("/search", {
        query: debounced,
        kinds: filterKinds.length ? filterKinds : null,
        skip: 0,
        take: 60,
      });
      return res.data;
    },
  });

  const toggleKind = (k: MediaKind) =>
    setFilterKinds((prev) => (prev.includes(k) ? prev.filter((x) => x !== k) : [...prev, k]));

  return (
    <>
      <Topbar />
      <PageScaffold>
        <div className="mx-auto max-w-3xl">
          <motion.h1
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-center text-4xl font-bold tracking-tight md:text-5xl"
          >
            <span className="gradient-text">Find anything</span>
          </motion.h1>
          <p className="mt-2 text-center text-sm text-mythra-text-muted">
            Search across movies, anime, manga, books, and audiobooks at once.
          </p>

          <div className="mt-8 flex items-center gap-3 rounded-2xl border border-white/[0.06] bg-white/[0.03] px-4 py-3 focus-within:border-mythra-purple/60">
            <Search size={18} className="text-mythra-text-muted" />
            <input
              autoFocus
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder='Try "time travel anime" or "dune"'
              className="flex-1 bg-transparent text-base text-white placeholder-mythra-text-soft outline-none"
            />
          </div>

          <div className="mt-3 flex flex-wrap gap-2">
            {KINDS.map((k) => {
              const active = filterKinds.includes(k);
              return (
                <button
                  key={k}
                  onClick={() => toggleKind(k)}
                  className={
                    "rounded-full border px-3 py-1 text-xs transition " +
                    (active
                      ? "border-mythra-purple/60 bg-mythra-purple/15 text-white"
                      : "border-white/[0.06] text-mythra-text-muted hover:bg-white/[0.05] hover:text-white")
                  }
                >
                  {k}
                </button>
              );
            })}
          </div>
        </div>

        <div className="mt-12">
          {search.data && search.data.hits.length === 0 && debounced && (
            <p className="text-center text-sm text-mythra-text-soft">No results for &ldquo;{debounced}&rdquo;.</p>
          )}
          {search.data && search.data.hits.length > 0 && (
            <>
              <p className="mb-4 text-xs text-mythra-text-soft">
                {search.data.total} results in {search.data.elapsedMs.toFixed(0)}ms
              </p>
              <motion.div
                initial="hidden"
                animate="visible"
                variants={{ hidden: {}, visible: { transition: { staggerChildren: 0.04 } } }}
                className="grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-5"
              >
                {search.data.hits.map((hit) => (
                  <motion.div
                    key={hit.id}
                    variants={{ hidden: { opacity: 0, y: 12 }, visible: { opacity: 1, y: 0 } }}
                  >
                    <MediaCard item={hit} size="sm" />
                  </motion.div>
                ))}
              </motion.div>
            </>
          )}
        </div>
      </PageScaffold>
    </>
  );
}
