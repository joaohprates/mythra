"use client";

import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { MediaCard } from "@/components/media/MediaCard";
import { api } from "@/lib/api";
import { useTranslation } from "@/store/locale";
import type { MediaItem, MediaKind, PagedResult } from "@/lib/types";
import type { TranslationKey } from "@/lib/i18n";

const KIND_LABEL_KEYS: Record<string, TranslationKey> = {
  Video: "kind.video",
  Manga: "kind.manga",
  Book:  "kind.book",
};

export default function LibraryByTypePage() {
  const params = useParams<{ type: string }>();
  const kind = params.type as MediaKind;
  const t = useTranslation();

  const items = useQuery({
    queryKey: ["library", "all", kind],
    queryFn: async () => {
      const res = await api.get<PagedResult<MediaItem>>("/items", {
        params: { kind, take: 200, includeAdult: true },
      });
      return res.data;
    },
    enabled: !!kind,
  });

  const labelKey = KIND_LABEL_KEYS[kind];
  const label = labelKey ? t(labelKey) : kind;

  return (
    <>
      <Topbar />
      <PageScaffold>
        <h1 className="mb-2 text-3xl font-bold tracking-tight md:text-4xl">
          <span className="gradient-text">{label}</span>
        </h1>
        <p className="text-sm text-mythra-text-muted">
          {items.data
            ? t(items.data.total === 1 ? "library.itemCount" : "library.itemsCount", { count: String(items.data.total) })
            : t("common.loading")}
        </p>

        {items.isLoading && (
          <div className="mt-16 text-center text-mythra-text-muted text-sm">{t("common.loading")}</div>
        )}

        <div className="mt-8 grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-5">
          {(items.data?.items ?? []).map((it) => (
            <MediaCard key={it.id} item={it} size="sm" />
          ))}
        </div>

        {!items.isLoading && (items.data?.items.length ?? 0) === 0 && (
          <div className="mt-24 text-center text-mythra-text-muted text-sm">
            {t("library.empty.cross", { kind: label.toLowerCase() })}{" "}
            <a href="/discover" className="text-mythra-purple underline-offset-2 hover:underline">
              {t("library.empty.discover")}
            </a>
            {" "}{t("library.empty.or")}{" "}
            <a href="/settings#libraries" className="text-mythra-purple underline-offset-2 hover:underline">
              {t("library.empty.localCta")}
            </a>
            .
          </div>
        )}
      </PageScaffold>
    </>
  );
}
