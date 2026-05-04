"use client";

import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { MediaCard } from "@/components/media/MediaCard";
import { api } from "@/lib/api";
import { useTranslation } from "@/store/locale";
import type { MediaItem, PagedResult } from "@/lib/types";

export default function LibraryPage() {
  const params = useParams<{ id: string }>();
  const isAll = params.id === "all";
  const t = useTranslation();

  const items = useQuery({
    queryKey: ["library", params.id],
    queryFn: async () => {
      const res = await api.get<PagedResult<MediaItem>>("/items", {
        params: {
          libraryId: isAll ? undefined : params.id,
          take: 200,
          includeAdult: true,
        },
      });
      return res.data;
    },
  });

  const title = isAll ? t("library.allItems") : t("library.title");

  return (
    <>
      <Topbar />
      <PageScaffold>
        <h1 className="mb-2 text-3xl font-bold tracking-tight md:text-4xl">
          <span className="gradient-text">{title}</span>
        </h1>
        <p className="text-sm text-mythra-text-muted">
          {items.data
            ? t(items.data.total === 1 ? "library.itemCount" : "library.itemsCount", { count: String(items.data.total) })
            : t("common.loading")}
        </p>

        <div className="mt-8 grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-5">
          {(items.data?.items ?? []).map((it) => (
            <MediaCard key={it.id} item={it} size="sm" />
          ))}
        </div>

        {!items.isLoading && (items.data?.items.length ?? 0) === 0 && (
          <div className="mt-24 text-center text-mythra-text-muted text-sm">
            {t("library.empty.add")}{" "}
            <a href="/settings" className="text-mythra-purple underline-offset-2 hover:underline">
              {t("nav.settings")}
            </a>
            .
          </div>
        )}
      </PageScaffold>
    </>
  );
}
