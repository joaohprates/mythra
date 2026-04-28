"use client";

import { useQuery } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { Bookmark, Headphones, Library, Play } from "lucide-react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";
import { Topbar } from "@/components/shell/Topbar";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";

interface ItemAny {
  id: string;
  kind: "Video" | "Manga" | "Book" | "Audio";
  title: string;
  overview?: string | null;
  posterPath?: string | null;
  backdropPath?: string | null;
  year?: number | null;
  rating?: number | null;
  genres?: string[];
  duration?: string | null;
  resolutionLabel?: string;
  author?: string | null;
  artist?: string | null;
  publisher?: string | null;
  pageCount?: number | null;
  totalChapters?: number | null;
  audioKind?: string;
  videoKind?: string;
  chapters?: { id: string; title: string }[];
}

export default function ItemDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const accessToken = useAuthStore((s) => s.accessToken);
  const isHydrated = useAuthStore((s) => s.isHydrated);

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const detail = useQuery({
    queryKey: ["item-detail", params.id],
    queryFn: async () => (await api.get<ItemAny>(`/items/${params.id}`)).data,
    enabled: !!params.id && !!accessToken,
  });

  const item = detail.data;

  return (
    <>
      <Topbar />
      <main className="relative min-h-[60vh]">
        {item && item.backdropPath && (
          <motion.div
            initial={{ opacity: 0, scale: 1.04 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 1.1, ease: [0.16, 1, 0.3, 1] }}
            className="absolute inset-x-0 top-0 -z-10 h-[70vh] overflow-hidden"
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={item.backdropPath} alt="" className="h-full w-full object-cover" />
            <div className="absolute inset-0 bg-gradient-to-b from-black/40 via-black/65 to-mythra-bg" />
          </motion.div>
        )}

        <section className="mx-auto max-w-[1500px] px-6 pb-24 pt-32 lg:px-10">
          {item ? (
            <motion.div
              initial={{ opacity: 0, y: 32, filter: "blur(8px)" }}
              animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
              transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1] }}
              className="grid gap-10 md:grid-cols-[280px_1fr]"
            >
              <div className="relative aspect-[2/3] overflow-hidden rounded-3xl border border-white/[0.06] shadow-mythra-card">
                {item.posterPath ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={item.posterPath} alt={item.title} className="h-full w-full object-cover" />
                ) : (
                  <div className="grid h-full w-full place-items-center bg-gradient-to-br from-[#1a1d35] to-[#070811] text-mythra-text-soft">
                    {item.kind}
                  </div>
                )}
              </div>

              <div>
                <span className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-[11px] uppercase tracking-widest text-mythra-text-soft">
                  {item.kind} {item.videoKind && `• ${item.videoKind}`} {item.audioKind && `• ${item.audioKind}`}
                </span>
                <h1 className="mt-4 text-4xl font-bold tracking-tight md:text-6xl">
                  <span className="gradient-text">{item.title}</span>
                </h1>
                <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-mythra-text-soft">
                  {item.year && <span>{item.year}</span>}
                  {item.author && <span>by {item.author}</span>}
                  {item.publisher && <span>{item.publisher}</span>}
                  {item.duration && <span>{formatDuration(item.duration)}</span>}
                  {item.resolutionLabel && <span className="rounded-full border border-white/10 px-2 py-0.5">{item.resolutionLabel}</span>}
                  {item.pageCount && <span>{item.pageCount} pages</span>}
                  {item.totalChapters && <span>{item.totalChapters} chapters</span>}
                  {item.rating && <span className="rounded-full bg-amber-300/15 px-2 py-0.5 text-amber-200">★ {item.rating.toFixed(1)}</span>}
                </div>
                {item.overview && <p className="mt-5 max-w-3xl text-sm leading-relaxed text-mythra-text-muted md:text-base">{item.overview}</p>}

                <div className="mt-7 flex flex-wrap gap-3">
                  <PrimaryAction kind={item.kind} id={item.id} />
                  <button className="inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.04] px-5 py-3 text-sm font-medium text-white backdrop-blur transition hover:bg-white/[0.08]">
                    <Bookmark size={16} /> Add to list
                  </button>
                  <Link
                    href={`/library/${item.kind}/all`}
                    className="inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.04] px-5 py-3 text-sm font-medium text-white backdrop-blur transition hover:bg-white/[0.08]"
                  >
                    <Library size={16} /> Library
                  </Link>
                </div>

                {item.genres && item.genres.length > 0 && (
                  <div className="mt-6 flex flex-wrap gap-2">
                    {item.genres.map((g) => (
                      <span key={g} className="rounded-full border border-white/10 px-3 py-1 text-xs text-mythra-text-muted">{g}</span>
                    ))}
                  </div>
                )}

                {item.chapters && item.chapters.length > 0 && (
                  <div className="mt-10">
                    <h3 className="mb-3 text-sm font-semibold uppercase tracking-widest text-mythra-text-soft">Chapters</h3>
                    <ul className="grid gap-2 md:grid-cols-2">
                      {item.chapters.slice(0, 12).map((c, i) => (
                        <li key={c.id} className="flex items-center gap-3 rounded-xl border border-white/[0.05] bg-white/[0.02] p-3">
                          <span className="grid h-7 w-7 place-items-center rounded-full bg-white/10 text-[11px]">{i + 1}</span>
                          <span className="text-sm text-white">{c.title}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </motion.div>
          ) : (
            <div className="flex h-[60vh] items-center justify-center text-mythra-text-soft">Loading…</div>
          )}
        </section>
      </main>
    </>
  );
}

function PrimaryAction({ kind, id }: { kind: string; id: string }) {
  const cfg = {
    Video: { href: `/watch/${id}`, icon: <Play size={16} className="fill-current" />, label: "Watch now" },
    Manga: { href: `/read/${id}`, icon: <Library size={16} />, label: "Read now" },
    Book: { href: `/read/${id}`, icon: <Library size={16} />, label: "Read now" },
    Audio: { href: `/listen/${id}`, icon: <Headphones size={16} />, label: "Listen now" },
  } as const;
  const c = cfg[kind as keyof typeof cfg] ?? cfg.Video;
  return (
    <Link
      href={c.href}
      className="inline-flex items-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold text-black shadow-[0_18px_50px_-15px_rgba(255,255,255,0.6)] transition hover:scale-[1.03]"
    >
      {c.icon} {c.label}
    </Link>
  );
}

function formatDuration(ts: string): string {
  const parts = ts.split(":");
  if (parts.length !== 3) return ts;
  const h = parseInt(parts[0], 10);
  const m = parseInt(parts[1], 10);
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
}
