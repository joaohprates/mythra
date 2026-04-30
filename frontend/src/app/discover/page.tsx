"use client";

import { motion, AnimatePresence } from "framer-motion";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  Search, Telescope, Star, BookOpen, Film, Music, BookImage,
  Download, Check, ExternalLink, Loader2,
} from "lucide-react";
import Link from "next/link";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import type { DiscoverItem, DiscoverResult, ImportResultDto, MediaKind } from "@/lib/types";

const KINDS: { value: MediaKind; label: string; icon: React.ReactNode }[] = [
  { value: "Video", label: "Movies & TV", icon: <Film size={14} /> },
  { value: "Book",  label: "Books",       icon: <BookOpen size={14} /> },
  { value: "Manga", label: "Manga",       icon: <BookImage size={14} /> },
  { value: "Audio", label: "Audiobooks",  icon: <Music size={14} /> },
];

export default function DiscoverPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);

  const [kind, setKind] = useState<MediaKind>("Video");
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const [importing, setImporting] = useState<Set<string>>(new Set());
  const [imported, setImported] = useState<Map<string, string>>(new Map()); // externalId → itemId

  useEffect(() => {
    if (!accessToken) router.replace("/login");
  }, [accessToken, router]);

  // Debounce query
  useEffect(() => {
    const t = setTimeout(() => setDebouncedQuery(query), 450);
    return () => clearTimeout(t);
  }, [query]);

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["discover", kind, debouncedQuery],
    queryFn: async () => {
      if (!debouncedQuery.trim()) return null;
      return (
        await api.get<DiscoverResult>("/discover", {
          params: { q: debouncedQuery, kind, skip: 0, take: 20 },
        })
      ).data;
    },
    enabled: !!accessToken && debouncedQuery.trim().length >= 2,
  });

  const importItem = async (item: DiscoverItem) => {
    setImporting((s) => new Set(s).add(item.externalId));
    try {
      const res = await api.post<ImportResultDto>("/discover/import", {
        externalId: item.externalId,
        providerKind: item.providerKind,
        mediaKind: kind,
        targetLibraryId: null,
      });
      setImported((m) => new Map(m).set(item.externalId, res.data.id));
    } catch {
      alert("Import failed. The item may already exist.");
    } finally {
      setImporting((s) => { const ns = new Set(s); ns.delete(item.externalId); return ns; });
    }
  };

  if (!accessToken) return null;

  const results = data?.items ?? [];
  const showEmpty = debouncedQuery.trim().length >= 2 && !isLoading && !isFetching && results.length === 0;

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <div className="flex items-center gap-3">
            <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-mythra-purple to-mythra-blue text-white">
              <Telescope size={20} />
            </span>
            <div>
              <h1 className="text-3xl font-bold tracking-tight md:text-4xl">
                <span className="gradient-text">Discover</span>
              </h1>
              <p className="text-sm text-mythra-text-muted">Search and import content from external providers.</p>
            </div>
          </div>
        </motion.div>

        {/* Kind tabs */}
        <div className="mt-8 flex gap-1 rounded-2xl border border-white/[0.06] bg-white/[0.02] p-1 w-fit">
          {KINDS.map((k) => (
            <button
              key={k.value}
              onClick={() => { setKind(k.value); setQuery(""); setDebouncedQuery(""); }}
              className={
                "flex items-center gap-1.5 rounded-xl px-4 py-2 text-sm font-medium transition-all " +
                (kind === k.value
                  ? "bg-gradient-to-r from-mythra-purple/30 to-mythra-blue/20 text-white"
                  : "text-mythra-text-muted hover:text-white")
              }
            >
              {k.icon} {k.label}
            </button>
          ))}
        </div>

        {/* Search bar */}
        <div className="mt-4 relative">
          <Search size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-mythra-text-muted" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={`Search ${KINDS.find((k) => k.value === kind)?.label.toLowerCase()}…`}
            autoFocus
            className="w-full rounded-2xl border border-white/10 bg-white/[0.03] py-3 pl-11 pr-4 text-sm outline-none placeholder:text-white/30 focus:border-mythra-purple/50 transition-colors"
          />
          {(isLoading || isFetching) && (
            <Loader2 size={16} className="absolute right-4 top-1/2 -translate-y-1/2 animate-spin text-mythra-text-muted" />
          )}
        </div>

        {/* Results */}
        {!debouncedQuery.trim() && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="mt-24 flex flex-col items-center gap-4 text-center"
          >
            <span className="grid h-20 w-20 place-items-center rounded-full bg-white/[0.03]">
              <Telescope size={36} className="text-mythra-text-muted" />
            </span>
            <p className="text-mythra-text-muted">Type to search for content to import into your library.</p>
          </motion.div>
        )}

        {showEmpty && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="mt-16 text-center text-mythra-text-muted text-sm"
          >
            No results found for "<span className="text-white">{debouncedQuery}</span>".
          </motion.div>
        )}

        <AnimatePresence mode="wait">
          {results.length > 0 && (
            <motion.div
              key={`${kind}-${debouncedQuery}`}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
            >
              {results.map((item) => (
                <DiscoverCard
                  key={item.externalId}
                  item={item}
                  isImporting={importing.has(item.externalId)}
                  importedId={imported.get(item.externalId) ?? item.existingItemId ?? null}
                  onImport={() => importItem(item)}
                />
              ))}
            </motion.div>
          )}
        </AnimatePresence>
      </PageScaffold>
    </>
  );
}

// ── Discover card ────────────────────────────────────────────────────────────

function DiscoverCard({
  item, isImporting, importedId, onImport,
}: {
  item: DiscoverItem;
  isImporting: boolean;
  importedId: string | null;
  onImport: () => void;
}) {
  const alreadyImported = !!(importedId ?? item.alreadyImported);

  return (
    <motion.div
      layout
      whileHover={{ y: -2 }}
      className="group relative overflow-hidden rounded-2xl border border-white/[0.06] bg-white/[0.02] transition-colors hover:border-white/10"
    >
      {/* Poster */}
      <div className="aspect-[2/3] overflow-hidden bg-white/[0.04]">
        {item.posterPath ? (
          <img
            src={item.posterPath}
            alt={item.title}
            className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-white/10">
            <Film size={40} />
          </div>
        )}
      </div>

      {/* Rating badge */}
      {item.rating != null && (
        <div className="absolute left-2 top-2 flex items-center gap-1 rounded-full bg-black/70 px-2 py-0.5 text-xs backdrop-blur">
          <Star size={10} className="text-amber-400" />
          <span>{item.rating.toFixed(1)}</span>
        </div>
      )}

      {/* Content */}
      <div className="p-3">
        <p className="line-clamp-1 text-sm font-medium text-white">{item.title}</p>
        {item.originalTitle && item.originalTitle !== item.title && (
          <p className="line-clamp-1 text-[11px] text-mythra-text-muted">{item.originalTitle}</p>
        )}
        <p className="mt-0.5 text-[11px] text-mythra-text-muted">
          {item.year ?? "—"}
          {item.genres.length > 0 && ` · ${item.genres.slice(0, 2).join(", ")}`}
        </p>

        {/* Actions */}
        <div className="mt-3 flex gap-2">
          {alreadyImported ? (
            <Link
              href={`/item/${importedId ?? item.existingItemId}`}
              className="flex flex-1 items-center justify-center gap-1.5 rounded-xl border border-emerald-500/30 bg-emerald-500/10 py-2 text-xs text-emerald-300"
            >
              <Check size={12} /> In library
            </Link>
          ) : (
            <button
              onClick={onImport}
              disabled={isImporting}
              className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-gradient-to-r from-mythra-purple to-mythra-blue py-2 text-xs font-medium text-white disabled:opacity-60"
            >
              {isImporting ? (
                <><Loader2 size={12} className="animate-spin" /> Importing…</>
              ) : (
                <><Download size={12} /> Import</>
              )}
            </button>
          )}
        </div>
      </div>
    </motion.div>
  );
}
