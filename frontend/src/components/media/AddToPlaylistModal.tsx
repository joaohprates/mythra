"use client";

import { motion, AnimatePresence } from "framer-motion";
import { ListPlus, Plus, Check, X } from "lucide-react";
import { useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useToasts } from "@/store/toasts";
import { cn } from "@/lib/cn";
import type { Playlist } from "@/lib/types";

interface Props {
  itemId: string;
  open: boolean;
  onClose: () => void;
}

export function AddToPlaylistModal({ itemId, open, onClose }: Props) {
  const profileId = useAuthStore((s) => s.activeProfile?.id);
  const qc = useQueryClient();
  const pushToast = useToasts((s) => s.push);
  const [newName, setNewName] = useState("");
  const [creating, setCreating] = useState(false);
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set());

  const playlists = useQuery({
    queryKey: ["playlists", profileId],
    queryFn: async () =>
      (await api.get<Playlist[]>(`/profiles/${profileId}/playlists`)).data,
    enabled: open && !!profileId,
    staleTime: 10_000,
  });

  const extractError = (error: unknown) => {
    const data = (error as { response?: { data?: { detail?: string; title?: string } } })?.response?.data;
    return data?.detail ?? data?.title;
  };

  const addItem = useMutation({
    mutationFn: async (playlistId: string) => {
      await api.post(`/profiles/${profileId}/playlists/${playlistId}/items`, {
        mediaItemId: itemId,
      });
      return playlistId;
    },
    onSuccess: (playlistId) => {
      setAddedIds((prev) => new Set(prev).add(playlistId));
      qc.invalidateQueries({ queryKey: ["playlists", profileId] });
    },
    onError: (error) => {
      pushToast({ kind: "error", message: extractError(error) ?? "Failed to add item to playlist." });
    },
  });

  const createPlaylist = useMutation({
    mutationFn: async () => {
      const res = await api.post<Playlist>(`/profiles/${profileId}/playlists`, {
        name: newName.trim(),
      });
      await api.post(`/profiles/${profileId}/playlists/${res.data.id}/items`, { mediaItemId: itemId });
      return res.data;
    },
    onSuccess: (playlist) => {
      setAddedIds((prev) => new Set(prev).add(playlist.id));
      setNewName("");
      setCreating(false);
      qc.invalidateQueries({ queryKey: ["playlists", profileId] });
    },
    onError: (error) => {
      pushToast({ kind: "error", message: extractError(error) ?? "Failed to create playlist." });
    },
  });

  if (!profileId || typeof document === "undefined") return null;

  return createPortal(
    <AnimatePresence>
      {open && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-[10000] bg-black/60 backdrop-blur-sm"
            onClick={onClose}
          />

          {/* Panel */}
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 16 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 16 }}
            transition={{ duration: 0.2, ease: [0.16, 1, 0.3, 1] }}
            className="fixed left-1/2 top-1/2 z-[10001] w-full max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-3xl border border-white/[0.08] bg-[#0c0e1a] shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className="flex items-center justify-between border-b border-white/[0.06] px-5 py-4">
              <div className="flex items-center gap-2">
                <ListPlus size={16} className="text-mythra-purple" />
                <span className="text-sm font-semibold">Add to Playlist</span>
              </div>
              <button
                onClick={onClose}
                className="grid h-7 w-7 place-items-center rounded-full text-mythra-text-muted hover:bg-white/[0.06] hover:text-white"
              >
                <X size={14} />
              </button>
            </div>

            {/* List */}
            <div className="max-h-64 overflow-y-auto p-2">
              {playlists.isLoading && (
                <p className="py-6 text-center text-xs text-mythra-text-soft">Loading…</p>
              )}
              {playlists.data?.length === 0 && !creating && (
                <p className="py-6 text-center text-xs text-mythra-text-soft">No playlists yet.</p>
              )}
              {playlists.data?.map((pl) => {
                const added = addedIds.has(pl.id);
                return (
                  <button
                    key={pl.id}
                    onClick={() => !added && addItem.mutate(pl.id)}
                    disabled={added || addItem.isPending}
                    className={cn(
                      "flex w-full items-center justify-between gap-3 rounded-xl px-3 py-2.5 text-left text-sm transition",
                      added
                        ? "text-mythra-purple"
                        : "text-mythra-text-muted hover:bg-white/[0.05] hover:text-white"
                    )}
                  >
                    <span className="truncate">{pl.name}</span>
                    {added ? (
                      <Check size={14} className="shrink-0 text-mythra-purple" />
                    ) : (
                      <span className="shrink-0 text-[10px] text-mythra-text-soft">
                        {pl.itemCount} items
                      </span>
                    )}
                  </button>
                );
              })}
            </div>

            {/* Create new */}
            <div className="border-t border-white/[0.06] p-3">
              {creating ? (
                <div className="flex items-center gap-2">
                  <input
                    autoFocus
                    value={newName}
                    onChange={(e) => setNewName(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" && newName.trim()) createPlaylist.mutate();
                      if (e.key === "Escape") setCreating(false);
                    }}
                    placeholder="Playlist name…"
                    className="flex-1 rounded-xl border border-white/[0.08] bg-white/[0.04] px-3 py-2 text-sm text-white placeholder-mythra-text-soft outline-none focus:border-mythra-purple/60"
                  />
                  <button
                    onClick={() => newName.trim() && createPlaylist.mutate()}
                    disabled={!newName.trim() || createPlaylist.isPending}
                    className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-mythra-purple/20 text-mythra-purple hover:bg-mythra-purple/30 disabled:opacity-40"
                  >
                    <Check size={14} />
                  </button>
                  <button
                    onClick={() => setCreating(false)}
                    className="grid h-9 w-9 shrink-0 place-items-center rounded-xl text-mythra-text-muted hover:bg-white/[0.06]"
                  >
                    <X size={14} />
                  </button>
                </div>
              ) : (
                <button
                  onClick={() => setCreating(true)}
                  className="flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-sm text-mythra-text-muted hover:bg-white/[0.05] hover:text-white"
                >
                  <Plus size={13} />
                  New playlist
                </button>
              )}
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>,
    document.body
  );
}
