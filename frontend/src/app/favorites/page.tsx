"use client";

import { motion } from "framer-motion";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Heart, Loader2 } from "lucide-react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { MediaCard } from "@/components/media/MediaCard";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import type { FavoriteItem, MediaItem } from "@/lib/types";

export default function FavoritesPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const activeProfile = useAuthStore((s) => s.activeProfile);
  const t = useTranslation();

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const favorites = useQuery({
    queryKey: ["favorites", activeProfile?.id],
    queryFn: async () => {
      if (!activeProfile?.id) return [];
      const favRes = await api.get<FavoriteItem[]>(`/profiles/${activeProfile.id}/favorites`);
      if (favRes.data.length === 0) return [];
      const ids = favRes.data.map((f) => f.mediaItemId).join(",");
      const itemsRes = await api.get<{ items: MediaItem[] }>("/items", { params: { ids, take: favRes.data.length } });
      const itemMap = new Map(itemsRes.data.items.map((i) => [i.id, i]));
      return favRes.data.map((f) => itemMap.get(f.mediaItemId)).filter((i): i is MediaItem => !!i);
    },
    enabled: !!activeProfile?.id && !!accessToken,
  });

  const items = favorites.data ?? [];

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <div className="flex items-center gap-3">
            <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-red-500/80 to-rose-600 text-white">
              <Heart size={20} className="fill-current" />
            </span>
            <div>
              <h1 className="text-3xl font-bold tracking-tight md:text-4xl">
                <span className="gradient-text">{t("favorites.title")}</span>
              </h1>
              <p className="text-sm text-mythra-text-muted">
                {items.length} {items.length === 1 ? t("common.item") : t("common.items")}
              </p>
            </div>
          </div>
        </motion.div>

        {favorites.isLoading && (
          <div className="mt-24 flex justify-center">
            <Loader2 size={28} className="animate-spin text-mythra-text-muted" />
          </div>
        )}

        {!favorites.isLoading && items.length === 0 && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="mt-24 flex flex-col items-center gap-4 text-center"
          >
            <span className="grid h-20 w-20 place-items-center rounded-full bg-white/[0.03]">
              <Heart size={36} className="text-mythra-text-muted" />
            </span>
            <p className="text-mythra-text-muted">{t("favorites.empty")}</p>
          </motion.div>
        )}

        {items.length > 0 && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="mt-8 grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5"
          >
            {items.map((item) => (
              <MediaCard key={item.id} item={item} size="sm" />
            ))}
          </motion.div>
        )}
      </PageScaffold>
    </>
  );
}
