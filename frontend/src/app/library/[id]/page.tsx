"use client";

import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { MediaCard } from "@/components/media/MediaCard";
import { api } from "@/lib/api";
import type { MediaItem, MediaKind, PagedResult } from "@/lib/types";

export default function LibraryPage() {
  const params = useParams<{ id: string }>();
  // Routes: /library/<libraryId> or /library/all/<kind>
  const isAll = params.id === "all";
  const segment = typeof window !== "undefined" ? window.location.pathname.split("/").pop() : null;
  const kindFilter = (isAll && segment ? segment : null) as MediaKind | null;

  const items = useQuery({
    queryKey: ["library", params.id, kindFilter],
    queryFn: async () => {
      const res = await api.get<PagedResult<MediaItem>>("/items", {
        params: {
          libraryId: isAll ? undefined : params.id,
          kind: kindFilter ?? undefined,
          take: 120,
        },
      });
      return res.data;
    },
  });

  const title = kindFilter ? `${kindFilter} Library` : isAll ? "All Items" : "Library";

  return (
    <>
      <Topbar />
      <PageScaffold>
        <h1 className="mb-2 text-3xl font-bold tracking-tight md:text-4xl">
          <span className="gradient-text">{title}</span>
        </h1>
        <p className="text-sm text-mythra-text-muted">
          {items.data ? `${items.data.total} item${items.data.total !== 1 ? "s" : ""}` : "Loading..."}
        </p>

        <div className="mt-8 grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-5">
          {(items.data?.items ?? []).map((it) => (
            <MediaCard key={it.id} item={it} size="sm" />
          ))}
        </div>
      </PageScaffold>
    </>
  );
}
