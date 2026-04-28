"use client";

import { useQuery } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { ChevronLeft } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { AudioPlayer } from "@/components/media/AudioPlayer";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import type { AudioItemDetail } from "@/lib/types";

export default function ListenPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const detail = useQuery({
    queryKey: ["audio-detail", params.id],
    queryFn: async () => (await api.get<AudioItemDetail>(`/items/${params.id}`)).data,
    enabled: !!params.id && !!accessToken,
  });

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.button
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          onClick={() => router.back()}
          className="mb-6 inline-flex items-center gap-2 text-sm text-mythra-text-muted hover:text-white"
        >
          <ChevronLeft size={16} /> Back
        </motion.button>

        {detail.data ? (
          <AudioPlayer audio={detail.data} />
        ) : (
          <div className="grid h-[60vh] place-items-center text-mythra-text-soft">Loading…</div>
        )}
      </PageScaffold>
    </>
  );
}
