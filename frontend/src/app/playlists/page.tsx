"use client";

import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { motion, AnimatePresence } from "framer-motion";
import {
  Plus, ListMusic, Trash2, Lock, Globe, X, Check,
  GripVertical, Loader2, ChevronLeft,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import type { Playlist, PlaylistDetail } from "@/lib/types";

// ── API helpers ──────────────────────────────────────────────────────────────

const playlistApi = {
  list: (profileId: string) =>
    api.get<Playlist[]>(`/profiles/${profileId}/playlists`).then((r) => r.data),
  get: (profileId: string, id: string) =>
    api.get<PlaylistDetail>(`/profiles/${profileId}/playlists/${id}`).then((r) => r.data),
  create: (profileId: string, data: { name: string; description?: string; isPublic?: boolean }) =>
    api.post<PlaylistDetail>(`/profiles/${profileId}/playlists`, data).then((r) => r.data),
  update: (profileId: string, id: string, data: Partial<{ name: string; description: string; isPublic: boolean }>) =>
    api.put<PlaylistDetail>(`/profiles/${profileId}/playlists/${id}`, data).then((r) => r.data),
  delete: (profileId: string, id: string) =>
    api.delete(`/profiles/${profileId}/playlists/${id}`),
  removeItem: (profileId: string, playlistId: string, mediaItemId: string) =>
    api.delete<PlaylistDetail>(`/profiles/${profileId}/playlists/${playlistId}/items/${mediaItemId}`).then((r) => r.data),
  reorder: (profileId: string, playlistId: string, data: { mediaItemId: string; newOrder: number }) =>
    api.patch<PlaylistDetail>(`/profiles/${profileId}/playlists/${playlistId}/items/reorder`, data).then((r) => r.data),
};

// ── Create modal ─────────────────────────────────────────────────────────────

function CreatePlaylistModal({ profileId, onClose }: { profileId: string; onClose: () => void }) {
  const qc = useQueryClient();
  const t = useTranslation();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isPublic, setIsPublic] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: () => playlistApi.create(profileId, { name: name.trim(), description: description.trim() || undefined, isPublic }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["playlists", profileId] });
      onClose();
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string; title?: string; error?: string } } })
          ?.response?.data;
      setErrorMsg(detail?.detail ?? detail?.title ?? detail?.error ?? t("playlists.error"));
    },
  });

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.92, opacity: 0, y: 16 }}
        animate={{ scale: 1, opacity: 1, y: 0 }}
        exit={{ scale: 0.92, opacity: 0, y: 16 }}
        transition={{ duration: 0.25, ease: [0.16, 1, 0.3, 1] }}
        className="w-full max-w-md mx-4 rounded-3xl border border-white/10 bg-[#0d0f1c] p-7 shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-lg font-bold text-white">{t("playlists.new")}</h2>
          <button
            onClick={onClose}
            className="grid h-8 w-8 place-items-center rounded-full text-mythra-text-muted transition hover:bg-white/[0.06] hover:text-white"
          >
            <X size={16} />
          </button>
        </div>

        <div className="space-y-4 mt-2">
          {errorMsg && (
            <div className="rounded-xl border border-rose-500/20 bg-rose-500/10 px-3 py-2 text-xs text-rose-300">
              {errorMsg}
            </div>
          )}
          <div>
            <label className="mb-1.5 block text-xs font-medium text-mythra-text-soft">
              {t("playlists.name")} *
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => { setName(e.target.value); setErrorMsg(null); }}
              onKeyDown={(e) => e.key === "Enter" && name.trim() && !create.isPending && create.mutate()}
              placeholder="Minha playlist"
              autoFocus
              className="w-full rounded-xl border border-white/10 bg-white/[0.04] px-3.5 py-2.5 text-sm text-white placeholder-white/20 outline-none transition focus:border-mythra-purple/50"
            />
          </div>
          <div>
            <label className="mb-1.5 block text-xs font-medium text-mythra-text-soft">
              {t("playlists.description")}
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={2}
              placeholder="Opcional"
              className="w-full resize-none rounded-xl border border-white/10 bg-white/[0.04] px-3.5 py-2.5 text-sm text-white placeholder-white/20 outline-none transition focus:border-mythra-purple/50"
            />
          </div>
          <button
            onClick={() => setIsPublic(!isPublic)}
            className="flex items-center gap-2 rounded-full border border-white/10 px-3 py-1.5 text-xs text-mythra-text-muted transition hover:border-white/20 hover:text-white"
          >
            {isPublic
              ? <Globe size={12} className="text-mythra-blue" />
              : <Lock size={12} />}
            {isPublic ? t("playlists.public") : t("playlists.private")}
          </button>
        </div>

        <div className="mt-7 flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 rounded-2xl border border-white/10 py-2.5 text-sm text-mythra-text-muted transition hover:border-white/20 hover:text-white"
          >
            {t("action.cancel")}
          </button>
          <button
            onClick={() => create.mutate()}
            disabled={!name.trim() || create.isPending}
            className="flex flex-1 items-center justify-center gap-2 rounded-2xl bg-white py-2.5 text-sm font-semibold text-black transition hover:bg-white/90 disabled:opacity-40"
          >
            {create.isPending ? <Loader2 size={14} className="animate-spin" /> : <Check size={14} />}
            {t("action.create")}
          </button>
        </div>
      </motion.div>
    </div>
  );
}

// ── Playlist card ─────────────────────────────────────────────────────────────

function PlaylistCard({
  playlist, profileId, onOpen,
}: {
  playlist: Playlist;
  profileId: string;
  onOpen: (id: string) => void;
}) {
  const qc = useQueryClient();
  const del = useMutation({
    mutationFn: () => playlistApi.delete(profileId, playlist.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["playlists", profileId] }),
  });

  return (
    <motion.div
      layout
      whileHover={{ y: -2 }}
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.95 }}
      className="group relative cursor-pointer overflow-hidden rounded-2xl border border-white/[0.06] bg-white/[0.02] p-5 transition-colors hover:border-white/10 hover:bg-white/[0.04]"
      onClick={() => onOpen(playlist.id)}
    >
      <div className="flex items-start gap-4">
        <div className="grid h-12 w-12 shrink-0 place-items-center rounded-xl bg-gradient-to-br from-mythra-purple/20 to-mythra-blue/20">
          <ListMusic size={20} className="text-mythra-purple" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <p className="truncate text-sm font-semibold text-white">{playlist.name}</p>
            {playlist.isPublic
              ? <Globe size={11} className="shrink-0 text-mythra-blue" />
              : <Lock size={11} className="shrink-0 text-mythra-text-muted" />}
          </div>
          {playlist.description && (
            <p className="mt-0.5 truncate text-xs text-mythra-text-muted">{playlist.description}</p>
          )}
          <p className="mt-1.5 text-[11px] text-mythra-text-soft">
            {playlist.itemCount} {playlist.itemCount === 1 ? "item" : "itens"}
          </p>
        </div>
      </div>

      <button
        className="absolute right-3 top-3 grid h-7 w-7 place-items-center rounded-full bg-white/0 opacity-0 text-mythra-text-muted transition-all group-hover:opacity-100 hover:bg-red-500/20 hover:text-red-400"
        onClick={(e) => { e.stopPropagation(); del.mutate(); }}
        title="Excluir playlist"
      >
        {del.isPending ? <Loader2 size={13} className="animate-spin" /> : <Trash2 size={13} />}
      </button>
    </motion.div>
  );
}

// ── Playlist detail ───────────────────────────────────────────────────────────

function PlaylistDetailView({
  profileId, playlistId, onBack,
}: {
  profileId: string;
  playlistId: string;
  onBack: () => void;
}) {
  const qc = useQueryClient();
  const router = useRouter();
  const t = useTranslation();

  const { data: playlist } = useQuery({
    queryKey: ["playlist", profileId, playlistId],
    queryFn: () => playlistApi.get(profileId, playlistId),
  });

  const removeItem = useMutation({
    mutationFn: (mediaItemId: string) => playlistApi.removeItem(profileId, playlistId, mediaItemId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["playlist", profileId, playlistId] });
      qc.invalidateQueries({ queryKey: ["playlists", profileId] });
    },
  });

  if (!playlist)
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 size={24} className="animate-spin text-mythra-text-muted" />
      </div>
    );

  const handlePlay = (item: { mediaItemId: string; kind: string }) => {
    const routes: Record<string, string> = {
      Video: `/watch/${item.mediaItemId}`,
      Audio: `/listen/${item.mediaItemId}`,
      Book:  `/read/${item.mediaItemId}`,
      Manga: `/read/${item.mediaItemId}`,
    };
    router.push(routes[item.kind] ?? `/item/${item.mediaItemId}`);
  };

  return (
    <motion.div
      initial={{ opacity: 0, x: 12 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -12 }}
      transition={{ duration: 0.25 }}
    >
      {/* Back button */}
      <button
        onClick={onBack}
        className="mb-6 inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.03] px-3.5 py-1.5 text-sm text-mythra-text-muted transition hover:bg-white/[0.07] hover:text-white"
      >
        <ChevronLeft size={14} /> {t("common.back")}
      </button>

      {/* Header */}
      <div className="mb-8 flex items-center gap-5">
        <div className="grid h-16 w-16 shrink-0 place-items-center rounded-2xl bg-gradient-to-br from-mythra-purple/25 to-mythra-blue/20">
          <ListMusic size={28} className="text-mythra-purple" />
        </div>
        <div>
          <h2 className="text-2xl font-bold tracking-tight text-white">{playlist.name}</h2>
          {playlist.description && (
            <p className="mt-0.5 text-sm text-mythra-text-muted">{playlist.description}</p>
          )}
          <p className="mt-1.5 text-xs text-mythra-text-soft">
            {playlist.items.length} {playlist.items.length === 1 ? "item" : "itens"}
          </p>
        </div>
      </div>

      {/* Items */}
      {playlist.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <ListMusic size={48} className="mb-4 text-mythra-text-muted/30" />
          <p className="font-medium text-mythra-text-muted">{t("playlists.empty")}</p>
          <p className="mt-1 text-sm text-mythra-text-soft">{t("playlists.emptyDetail")}</p>
        </div>
      ) : (
        <div className="space-y-1">
          {playlist.items.map((item, idx) => (
            <motion.div
              key={item.id}
              initial={{ opacity: 0, x: -8 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: idx * 0.03 }}
              className="group flex items-center gap-3 rounded-xl border border-transparent p-3 transition-colors hover:border-white/[0.05] hover:bg-white/[0.03]"
            >
              <GripVertical size={14} className="shrink-0 cursor-grab text-mythra-text-muted/40" />
              <div
                className="flex flex-1 cursor-pointer items-center gap-3"
                onClick={() => handlePlay(item)}
              >
                {item.posterPath ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={item.posterPath}
                    alt={item.title}
                    className="h-14 w-10 rounded-lg object-cover shadow"
                  />
                ) : (
                  <div className="flex h-14 w-10 items-center justify-center rounded-lg bg-white/[0.06]">
                    <ListMusic size={14} className="text-mythra-text-muted" />
                  </div>
                )}
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-white">{item.title}</p>
                  <p className="text-xs text-mythra-text-soft">
                    {item.kind}{item.year ? ` · ${item.year}` : ""}
                  </p>
                </div>
              </div>
              <button
                className="grid h-7 w-7 shrink-0 place-items-center rounded-full opacity-0 text-mythra-text-muted transition-all group-hover:opacity-100 hover:bg-red-500/15 hover:text-red-400"
                onClick={() => removeItem.mutate(item.mediaItemId)}
                title="Remover da playlist"
              >
                <X size={13} />
              </button>
            </motion.div>
          ))}
        </div>
      )}
    </motion.div>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export default function PlaylistsPage() {
  const { activeProfile, user, accessToken, isHydrated } = useAuthStore();
  // Fallback to first profile from user object if activeProfile not yet set
  const activeProfileId = activeProfile?.id ?? user?.profiles?.[0]?.id ?? null;
  const hasProfiles = (user?.profiles?.length ?? 0) > 0;
  const router = useRouter();
  const t = useTranslation();
  const [showCreate, setShowCreate] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Auth guard — must be a useEffect so hooks are called unconditionally
  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const { data: playlists = [], isLoading } = useQuery({
    queryKey: ["playlists", activeProfileId],
    queryFn: () => (activeProfileId ? playlistApi.list(activeProfileId) : Promise.resolve([])),
    enabled: !!activeProfileId && !!accessToken,
  });

  return (
    <>
      <Topbar />
      <PageScaffold>
        <AnimatePresence mode="wait">
          {selectedId && activeProfileId ? (
            <PlaylistDetailView
              key="detail"
              profileId={activeProfileId}
              playlistId={selectedId}
              onBack={() => setSelectedId(null)}
            />
          ) : (
            <motion.div
              key="list"
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ duration: 0.3 }}
            >
              {/* ── Page header ── */}
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-center gap-3">
                  <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-mythra-purple to-mythra-blue text-white">
                    <ListMusic size={20} />
                  </span>
                  <div>
                    <h1 className="text-3xl font-bold tracking-tight md:text-4xl">
                      <span className="gradient-text">{t("playlists.title")}</span>
                    </h1>
                    <p className="text-sm text-mythra-text-muted">
                      Organize seu conteúdo favorito
                    </p>
                  </div>
                </div>

                {activeProfileId && (
                  <button
                    onClick={() => setShowCreate(true)}
                    className="inline-flex shrink-0 items-center gap-2 rounded-full bg-white px-5 py-2.5 text-sm font-semibold text-black shadow-[0_10px_30px_-10px_rgba(255,255,255,0.4)] transition hover:scale-[1.02]"
                  >
                    <Plus size={15} /> {t("playlists.new")}
                  </button>
                )}
              </div>

              {/* ── No profile ── */}
              {!activeProfileId && !isLoading && (
                <div className="mt-24 flex flex-col items-center gap-4 text-center">
                  <ListMusic size={40} className="text-mythra-text-muted" />
                  {!hasProfiles ? (
                    <>
                      <p className="text-mythra-text-muted">Você ainda não tem perfis criados.</p>
                      <a href="/settings#profiles" className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-5 py-2.5 text-sm text-white hover:bg-white/[0.08]">
                        Criar um perfil em Configurações
                      </a>
                    </>
                  ) : (
                    <p className="text-mythra-text-muted">Aguardando perfil ativo…</p>
                  )}
                </div>
              )}

              {/* ── Loading ── */}
              {activeProfileId && isLoading && (
                <div className="mt-24 flex justify-center">
                  <Loader2 size={28} className="animate-spin text-mythra-text-muted" />
                </div>
              )}

              {/* ── Empty ── */}
              {activeProfileId && !isLoading && playlists.length === 0 && (
                <div className="mt-24 flex flex-col items-center gap-4 text-center">
                  <span className="grid h-20 w-20 place-items-center rounded-full bg-white/[0.03]">
                    <ListMusic size={36} className="text-mythra-text-muted" />
                  </span>
                  <div>
                    <p className="font-semibold text-mythra-text-muted">{t("playlists.empty")}</p>
                  </div>
                  <button
                    onClick={() => setShowCreate(true)}
                    className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-5 py-2.5 text-sm text-mythra-text-muted transition hover:bg-white/[0.08] hover:text-white"
                  >
                    <Plus size={14} /> {t("playlists.new")}
                  </button>
                </div>
              )}

              {/* ── Grid ── */}
              {activeProfileId && !isLoading && playlists.length > 0 && (
                <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                  <AnimatePresence>
                    {playlists.map((p) => (
                      <PlaylistCard
                        key={p.id}
                        playlist={p}
                        profileId={activeProfileId}
                        onOpen={setSelectedId}
                      />
                    ))}
                  </AnimatePresence>
                </div>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </PageScaffold>

      <AnimatePresence>
        {showCreate && activeProfileId && (
          <CreatePlaylistModal
            profileId={activeProfileId}
            onClose={() => setShowCreate(false)}
          />
        )}
      </AnimatePresence>
    </>
  );
}
