"use client";

import { motion } from "framer-motion";
import { Download, Loader2, X } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { cn } from "@/lib/cn";
import { formatBytes } from "@/lib/text";
import { useTranslation } from "@/store/locale";

export interface DownloadVariant {
  size?: number | null;
  ext?: string | null;
  language?: string | null;
  quality?: string | null;
  url?: string | null;
}

interface DownloadOptionsDto {
  variants: DownloadVariant[];
}

interface FallbackInfo {
  fileSize?: number | null;
  fileExtension?: string | null;
  language?: string | null;
}

interface Props {
  itemId: string;
  open: boolean;
  onClose: () => void;
  fallback?: FallbackInfo;
  /** When true, no streaming addon is registered for this kind. Show unavailable state. */
  noAddon?: boolean;
}

export function DownloadDialog({ itemId, open, onClose, fallback, noAddon }: Props) {
  const t = useTranslation();
  const [variants, setVariants] = useState<DownloadVariant[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState(0);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setLoading(true);
    setSelected(0);
    (async () => {
      try {
        const res = await api.get<DownloadOptionsDto>(`/items/${itemId}/download-options`);
        if (cancelled) return;
        const list = res.data?.variants ?? [];
        setVariants(list.length > 0 ? list : buildFallback(fallback));
      } catch {
        if (cancelled) return;
        setVariants(buildFallback(fallback));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open, itemId, fallback]);

  if (!open) return null;

  const list = variants ?? [];
  const variant = list[selected];

  const handleConfirm = () => {
    if (!variant) return;
    const url = variant.url && variant.url.length > 0
      ? variant.url
      : `/api/v1/download/${itemId}${variant.language ? `?lang=${encodeURIComponent(variant.language)}` : ""}`;
    if (typeof window !== "undefined") {
      window.location.href = url;
    }
    onClose();
  };

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <motion.div
        initial={{ scale: 0.94, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        exit={{ scale: 0.94, opacity: 0 }}
        className="w-full max-w-lg rounded-3xl border border-white/[0.08] bg-[#0c0e1a] p-6 shadow-2xl"
      >
        <div className="flex items-start justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <Download size={18} />
            {t("download.title")}
          </h2>
          <button onClick={onClose} className="rounded-full p-1.5 hover:bg-white/10" aria-label={t("download.cancel")}>
            <X size={16} />
          </button>
        </div>

        {noAddon ? (
          <div className="mt-6 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-100">
            <p>{t("download.unavailable")}</p>
            <Link
              href="/settings#addons"
              className="mt-3 inline-block rounded-full border border-white/15 bg-white/[0.06] px-3 py-1.5 text-xs font-medium text-white hover:bg-white/[0.12]"
              onClick={onClose}
            >
              {t("download.openSettings")}
            </Link>
          </div>
        ) : loading ? (
          <div className="mt-6 flex items-center justify-center gap-2 text-sm text-mythra-text-muted">
            <Loader2 size={14} className="animate-spin" /> {t("common.loading")}
          </div>
        ) : list.length === 0 ? (
          <div className="mt-6 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-100">
            <p>{t("download.unavailable")}</p>
          </div>
        ) : (
          <ul className="mt-6 space-y-2">
            {list.map((v, idx) => (
              <li key={idx}>
                <button
                  onClick={() => setSelected(idx)}
                  className={cn(
                    "flex w-full items-center gap-3 rounded-2xl border px-4 py-3 text-left transition",
                    selected === idx
                      ? "border-mythra-purple/50 bg-mythra-purple/10"
                      : "border-white/[0.06] bg-white/[0.02] hover:bg-white/[0.05]"
                  )}
                >
                  <span
                    className={cn(
                      "grid h-4 w-4 place-items-center rounded-full border",
                      selected === idx ? "border-mythra-purple" : "border-white/30"
                    )}
                  >
                    {selected === idx && <span className="h-2 w-2 rounded-full bg-mythra-purple" />}
                  </span>
                  <div className="flex flex-1 flex-col min-w-0">
                    <div className="flex items-center gap-2 text-sm font-medium">
                      <span>{v.language?.toUpperCase() || t("download.unknownLang")}</span>
                      {v.ext && (
                        <span className="rounded-full border border-white/10 bg-white/[0.05] px-2 py-0.5 text-[10px] uppercase tracking-widest text-mythra-text-soft">
                          {v.ext.replace(/^\./, "")}
                        </span>
                      )}
                      {v.quality && (
                        <span className="rounded-full border border-mythra-purple/30 bg-mythra-purple/10 px-2 py-0.5 text-[10px] font-semibold text-mythra-purple">
                          {v.quality}
                        </span>
                      )}
                    </div>
                    <span className="text-xs text-mythra-text-muted">
                      {t("download.size")}: {formatBytes(v.size ?? null)}
                    </span>
                  </div>
                </button>
              </li>
            ))}
          </ul>
        )}

        {!noAddon && list.length > 0 && (
          <div className="mt-6 flex justify-end gap-2">
            <button
              onClick={onClose}
              className="rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-sm hover:bg-white/[0.08]"
            >
              {t("download.cancel")}
            </button>
            <button
              onClick={handleConfirm}
              className="inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2 text-sm font-medium text-white"
            >
              <Download size={14} /> {t("download.confirm")}
            </button>
          </div>
        )}
      </motion.div>
    </motion.div>
  );
}

function buildFallback(fallback?: FallbackInfo): DownloadVariant[] {
  const ext = fallback?.fileExtension ?? null;
  const size = fallback?.fileSize ?? null;
  const language = fallback?.language ?? null;
  if (ext == null && size == null && language == null) {
    return [{ ext: null, size: null, language: null, quality: null, url: null }];
  }
  return [{ ext, size, language, quality: null, url: null }];
}
