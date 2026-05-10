"use client";

import { useInfiniteQuery } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { Loader2, Search } from "lucide-react";
import { useEffect, useState } from "react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { MediaCard } from "@/components/media/MediaCard";
import { api } from "@/lib/api";
import { useTranslation } from "@/store/locale";
import type { MediaKind, SearchHit, SearchResult } from "@/lib/types";

const KINDS: MediaKind[] = ["Video", "Manga", "Book"];
const PAGE_SIZE = 24;

export default function SearchPage() {
  const t = useTranslation();
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [filterKinds, setFilterKinds] = useState<MediaKind[]>([]);

  useEffect(() => {
    const id = setTimeout(() => setDebounced(query), 300);
    return () => clearTimeout(id);
  }, [query]);

  const trimmedQuery = debounced.trim();

  const search = useInfiniteQuery({
    queryKey: ["search", trimmedQuery, filterKinds],
    queryFn: async ({ pageParam }) => {
      const res = await api.post<SearchResult>("/search", {
        query: trimmedQuery,
        kinds: filterKinds.length ? filterKinds : null,
        skip: pageParam,
        take: PAGE_SIZE,
      });
      return res.data;
    },
    initialPageParam: 0,
    getNextPageParam: (lastPage, allPages) => {
      const loaded = allPages.reduce((sum, p) => sum + p.hits.length, 0);
      return loaded < lastPage.total ? loaded : undefined;
    },
    staleTime: 30_000,
  });

  const allHits: SearchHit[] = search.data?.pages.flatMap((p) => p.hits) ?? [];
  const total = search.data?.pages[0]?.total ?? 0;
  const elapsedMs = search.data?.pages[search.data.pages.length - 1]?.elapsedMs ?? 0;
  const hasMore = search.hasNextPage;
  const isLoadingMore = search.isFetchingNextPage;

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
            <span className="gradient-text">{t("search.title")}</span>
          </motion.h1>
          <p className="mt-2 text-center text-sm text-mythra-text-muted">
            {t("search.subtitle")}
          </p>

          <div className="mt-8 flex items-center gap-3 rounded-2xl border border-white/[0.06] bg-white/[0.03] px-4 py-3 focus-within:border-mythra-purple/60">
            <Search size={18} className="text-mythra-text-muted" />
            <input
              autoFocus
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={t("search.placeholder")}
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
          {allHits.length === 0 && trimmedQuery.length > 0 && !search.isFetching && (
            <p className="text-center text-sm text-mythra-text-soft">
              {t("search.noResults", { query: trimmedQuery })}
            </p>
          )}

          {allHits.length > 0 && (
            <>
              <p className="mb-4 text-xs text-mythra-text-soft">
                {t("search.results", {
                  total: String(total),
                  ms: elapsedMs.toFixed(0),
                })}
              </p>
              <div className="grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-5">
                {allHits.map((hit, i) => (
                  <motion.div
                    key={hit.id}
                    initial={{ opacity: 0, y: 12 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.25, delay: Math.min(i % PAGE_SIZE, 12) * 0.04 }}
                  >
                    <MediaCard item={hit} size="sm" />
                  </motion.div>
                ))}
              </div>

              {hasMore && (
                <div className="mt-10 flex justify-center">
                  <button
                    onClick={() => search.fetchNextPage()}
                    disabled={isLoadingMore}
                    className="inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.04] px-6 py-3 text-sm font-medium text-white backdrop-blur transition hover:bg-white/[0.08] disabled:opacity-50"
                  >
                    {isLoadingMore ? (
                      <><Loader2 size={15} className="animate-spin" /> Carregando…</>
                    ) : (
                      "Carregar mais"
                    )}
                  </button>
                </div>
              )}
            </>
          )}

          {search.isFetching && !isLoadingMore && (
            <div className="flex justify-center py-12">
              <Loader2 size={24} className="animate-spin text-mythra-text-muted" />
            </div>
          )}
        </div>
      </PageScaffold>
    </>
  );
}
