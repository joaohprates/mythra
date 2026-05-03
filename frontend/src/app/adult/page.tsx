"use client";

import { motion } from "framer-motion";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ShieldAlert, Film, BookImage, BookOpen, Music, Settings } from "lucide-react";
import Link from "next/link";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { ContentRow } from "@/components/media/ContentRow";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useProfilePrefs } from "@/store/profile";
import { useTranslation } from "@/store/locale";
import type { MediaItem } from "@/lib/types";

export default function AdultPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const authHydrated = useAuthStore((s) => s.isHydrated);
  const prefsHydrated = useProfilePrefs((s) => s.isHydrated);
  const { showAdultContent } = useProfilePrefs();
  const t = useTranslation();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    if (!authHydrated || !prefsHydrated) return;
    if (!accessToken) { router.replace("/login"); return; }
    setReady(true);
  }, [authHydrated, prefsHydrated, accessToken, router]);

  const enabled = ready && showAdultContent && !!accessToken;

  const videos = useQuery({
    queryKey: ["adult", "video"],
    queryFn: async () =>
      (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Video", isAdult: true, take: 18 } })).data.items,
    enabled,
  });

  const manga = useQuery({
    queryKey: ["adult", "manga"],
    queryFn: async () =>
      (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Manga", isAdult: true, take: 18 } })).data.items,
    enabled,
  });

  const books = useQuery({
    queryKey: ["adult", "book"],
    queryFn: async () =>
      (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Book", isAdult: true, take: 18 } })).data.items,
    enabled,
  });

  const audio = useQuery({
    queryKey: ["adult", "audio"],
    queryFn: async () =>
      (await api.get<{ items: MediaItem[] }>("/items", { params: { kind: "Audio", isAdult: true, take: 18 } })).data.items,
    enabled,
  });

  if (!ready) return null;

  // Gate — adult content disabled
  if (!showAdultContent) {
    return (
      <>
        <Topbar />
        <PageScaffold>
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            className="mx-auto max-w-md py-24 text-center"
          >
            <div className="mx-auto mb-6 grid h-20 w-20 place-items-center rounded-full border border-red-500/20 bg-red-500/10">
              <ShieldAlert size={36} className="text-red-400" />
            </div>
            <h1 className="text-2xl font-bold text-white">{t("adult.section.title")}</h1>
            <p className="mt-3 text-sm text-mythra-text-muted">{t("adult.gate.message")}</p>
            <Link
              href="/settings#adult"
              className="mt-6 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-6 py-3 text-sm font-medium text-white"
            >
              <Settings size={14} /> {t("adult.gate.button")}
            </Link>
          </motion.div>
        </PageScaffold>
      </>
    );
  }

  const hasContent =
    (videos.data?.length ?? 0) +
    (manga.data?.length ?? 0) +
    (books.data?.length ?? 0) +
    (audio.data?.length ?? 0) > 0;

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <div className="flex items-center gap-3">
            <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-red-500/80 to-rose-600 text-white">
              <ShieldAlert size={20} />
            </span>
            <div>
              <h1 className="text-3xl font-bold tracking-tight md:text-4xl">
                <span className="gradient-text">{t("adult.section.title")}</span>
              </h1>
              <p className="text-sm text-mythra-text-muted">{t("adult.section.subtitle")}</p>
            </div>
          </div>
        </motion.div>

        {/* Category quick links */}
        <div className="mt-8 grid grid-cols-2 gap-3 sm:grid-cols-4">
          {[
            { icon: <Film size={16} />, label: t("kind.video"),  href: "#video" },
            { icon: <BookImage size={16} />, label: t("kind.manga"), href: "#manga" },
            { icon: <BookOpen size={16} />, label: t("kind.book"),  href: "#books" },
            { icon: <Music size={16} />, label: t("kind.audio"),  href: "#audio" },
          ].map((c) => (
            <a
              key={c.href}
              href={c.href}
              className="flex items-center gap-2.5 rounded-2xl border border-white/[0.06] bg-white/[0.02] px-4 py-3 text-sm text-mythra-text-muted transition hover:bg-white/[0.05] hover:text-white"
            >
              {c.icon} {c.label}
            </a>
          ))}
        </div>

        {!hasContent &&
          !videos.isLoading && !manga.isLoading && !books.isLoading && !audio.isLoading && (
          <div className="mt-24 flex flex-col items-center gap-4 text-center">
            <ShieldAlert size={48} className="text-mythra-text-muted/20" />
            <p className="text-mythra-text-muted">{t("library.empty")}</p>
            <p className="text-sm text-mythra-text-soft">
              Import adult content from{" "}
              <Link href="/discover" className="text-mythra-purple underline-offset-2 hover:underline">
                Discover
              </Link>
              .
            </p>
          </div>
        )}

        <div className="mt-10 space-y-12" id="video">
          {(videos.data?.length ?? 0) > 0 && (
            <ContentRow title={t("kind.video")} items={videos.data ?? []} size="md" loading={videos.isLoading} />
          )}
        </div>
        <div className="space-y-12 mt-12" id="manga">
          {(manga.data?.length ?? 0) > 0 && (
            <ContentRow title={t("kind.manga")} items={manga.data ?? []} size="sm" loading={manga.isLoading} />
          )}
        </div>
        <div className="space-y-12 mt-12" id="books">
          {(books.data?.length ?? 0) > 0 && (
            <ContentRow title={t("kind.book")} items={books.data ?? []} size="sm" loading={books.isLoading} />
          )}
        </div>
        <div className="space-y-12 mt-12" id="audio">
          {(audio.data?.length ?? 0) > 0 && (
            <ContentRow title={t("kind.audio")} items={audio.data ?? []} size="md" loading={audio.isLoading} />
          )}
        </div>
      </PageScaffold>
    </>
  );
}
