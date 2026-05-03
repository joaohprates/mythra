"use client";

import { motion, AnimatePresence } from "framer-motion";
import { Play, Heart, MoreVertical, ListPlus, Trash2, Library } from "lucide-react";
import Link from "next/link";
import { useState, useRef, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { cardHover } from "@/lib/motion";
import type { MediaItem, SearchHit } from "@/lib/types";
import { cn } from "@/lib/cn";
import { api } from "@/lib/api";
import { cleanDescription } from "@/lib/text";
import { useAuthStore } from "@/store/auth";
import { useProfilePrefs } from "@/store/profile";
import { useTranslation } from "@/store/locale";

type Item = MediaItem | SearchHit;

interface Props {
  item: Item;
  size?: "sm" | "md" | "lg";
  showOverview?: boolean;
  showActions?: boolean;
}

export function MediaCard({ item, size = "md", showOverview = false, showActions = true }: Props) {
  const aspect = "aspect-[2/3]";
  const sizes = {
    sm: "min-w-[180px] w-[180px]",
    md: "min-w-[260px] w-[260px]",
    lg: "min-w-[340px] w-[340px]",
  };
  const poster   = item.posterPath ?? null;
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
              <p className="mt-2 line-clamp-2 text-xs text-mythra-text-soft">{cleanDescription(overview)}</p>
            )}
          </div>
          <KindBadge kind={item.kind} />
          {"isAdult" in item && item.isAdult && (
            <AdultBadge />
          )}
          {"rating" in item && item.rating ? (
            <span className="absolute right-2 top-2 rounded-full bg-black/60 backdrop-blur px-2 py-0.5 text-[11px] font-semibold text-amber-200">
              {item.rating.toFixed(1)}
            </span>
          ) : null}
        </div>
      </Link>

      {/* Action overlay */}
      {showActions && "libraryId" in item && (
        <CardActions item={item as MediaItem} />
      )}
    </motion.div>
  );
}

// ── Card action menu ──────────────────────────────────────────────────────────

function CardActions({ item }: { item: MediaItem }) {
  const [open, setOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const activeProfile = useAuthStore((s) => s.activeProfile);
  const t = useTranslation();
  const qc = useQueryClient();

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setOpen(false);
    };
    if (open) document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

  const profileId = activeProfile?.id ?? null;

  // Favorite status
  const favoriteQuery = useQuery({
    queryKey: ["favorite-status", profileId, item.id],
    queryFn: async () => {
      if (!profileId) return { isFavorite: false };
      const res = await api.get<{ isFavorite: boolean }>(`/profiles/${profileId}/favorites/${item.id}/status`);
      return res.data;
    },
    enabled: !!profileId && open,
    staleTime: 30_000,
  });

  const isFavorite = favoriteQuery.data?.isFavorite ?? false;

  const toggleFavorite = useMutation({
    mutationFn: async () => {
      if (!profileId) return;
      if (isFavorite) {
        await api.delete(`/profiles/${profileId}/favorites/${item.id}`);
      } else {
        await api.post(`/profiles/${profileId}/favorites`, { mediaItemId: item.id });
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["favorite-status", profileId, item.id] });
      qc.invalidateQueries({ queryKey: ["favorites", profileId] });
    },
  });

  if (!profileId) return null;

  return (
    <div className="absolute right-2 top-2" ref={menuRef}>
      {/* Favorite button (always visible on hover) */}
      <button
        onClick={(e) => { e.preventDefault(); toggleFavorite.mutate(); }}
        className={cn(
          "absolute right-8 top-0 grid h-7 w-7 place-items-center rounded-full transition-all",
          "opacity-0 group-hover:opacity-100",
          isFavorite
            ? "bg-red-500/20 text-red-400"
            : "bg-black/50 text-white/60 hover:text-red-400"
        )}
        title={isFavorite ? t("action.unfavorite") : t("action.favorite")}
      >
        <Heart size={13} className={cn("transition-all", isFavorite && "fill-current")} />
      </button>

      {/* Three-dot menu */}
      <button
        onClick={(e) => { e.preventDefault(); setOpen((o) => !o); }}
        className="grid h-7 w-7 place-items-center rounded-full bg-black/50 text-white/60 opacity-0 group-hover:opacity-100 transition-all hover:bg-black/70 hover:text-white"
      >
        <MoreVertical size={13} />
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, scale: 0.92, y: -4 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.92, y: -4 }}
            transition={{ duration: 0.12 }}
            className="absolute right-0 top-8 z-50 w-44 overflow-hidden rounded-2xl border border-white/10 bg-[#0d0f1c] shadow-2xl"
            onClick={(e) => e.preventDefault()}
          >
            <MenuItem
              icon={<Heart size={13} className={cn(isFavorite && "fill-current text-red-400")} />}
              label={isFavorite ? t("action.unfavorite") : t("action.favorite")}
              onClick={() => { toggleFavorite.mutate(); setOpen(false); }}
            />
            <MenuItem
              icon={<Library size={13} />}
              label={t("action.viewInLibrary")}
              onClick={() => { window.location.href = `/item/${item.id}`; setOpen(false); }}
            />
            <MenuItem
              icon={<ListPlus size={13} />}
              label={t("action.addToPlaylist")}
              onClick={() => { window.location.href = `/playlists`; setOpen(false); }}
            />
            <div className="my-1 h-px bg-white/[0.06]" />
            <MenuItem
              icon={<Trash2 size={13} />}
              label={deleting ? "…" : t("action.removeFromLib")}
              danger
              onClick={async () => {
                if (!confirm(`Remove "${item.title}" from library?`)) return;
                setDeleting(true);
                try {
                  await api.delete(`/items/${item.id}`);
                  qc.invalidateQueries();
                } catch { /* silently fail for non-admins */ }
                finally { setDeleting(false); setOpen(false); }
              }}
            />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function MenuItem({
  icon, label, onClick, danger,
}: {
  icon: React.ReactNode;
  label: string;
  onClick: () => void;
  danger?: boolean;
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "flex w-full items-center gap-2.5 px-3.5 py-2.5 text-xs transition-colors hover:bg-white/[0.06]",
        danger ? "text-rose-400 hover:text-rose-300" : "text-mythra-text-muted hover:text-white"
      )}
    >
      {icon}
      {label}
    </button>
  );
}

function KindBadge({ kind }: { kind: string }) {
  const palette: Record<string, string> = {
    Video: "from-purple-500 to-blue-500",
    Manga: "from-rose-500 to-pink-600",
    Book:  "from-amber-400 to-orange-500",
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

function AdultBadge() {
  const { showAdultContent } = useProfilePrefs();
  if (!showAdultContent) return null;
  return (
    <span className="absolute left-3 top-10 rounded-full border border-red-400/30 bg-red-500/20 px-1.5 py-0.5 text-[9px] font-bold text-red-300 backdrop-blur">
      +18
    </span>
  );
}

function hrefFor(item: Item): string {
  return `/item/${item.id}`;
}
