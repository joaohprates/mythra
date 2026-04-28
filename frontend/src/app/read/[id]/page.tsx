"use client";

import { useQuery } from "@tanstack/react-query";
import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";
import { ChevronLeft } from "lucide-react";
import { motion } from "framer-motion";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { MangaReader } from "@/components/media/MangaReader";
import { BookReader } from "@/components/media/BookReader";
import type { BookItemDetail, MangaItemDetail } from "@/lib/types";

export default function ReadPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const detail = useQuery({
    queryKey: ["read-detail", params.id],
    queryFn: async () => (await api.get<MangaItemDetail | BookItemDetail>(`/items/${params.id}`)).data,
    enabled: !!params.id && !!accessToken,
  });

  if (!detail.data)
    return (
      <div className="grid min-h-screen place-items-center text-mythra-text-soft">
        Loading…
      </div>
    );

  return (
    <main className="relative min-h-screen">
      <motion.button
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        onClick={() => router.back()}
        className="absolute left-6 top-6 z-40 inline-flex items-center gap-2 rounded-full border border-white/10 bg-black/40 px-4 py-2 text-sm text-white/80 backdrop-blur transition hover:bg-white/10"
      >
        <ChevronLeft size={16} /> Back
      </motion.button>

      {detail.data.kind === "Manga" ? (
        <MangaReader manga={detail.data as MangaItemDetail} />
      ) : detail.data.kind === "Book" ? (
        <BookReader book={detail.data as BookItemDetail} />
      ) : (
        <div className="grid min-h-screen place-items-center text-mythra-text-soft">
          This media kind is not readable.
        </div>
      )}
    </main>
  );
}
