"use client";

import { AnimatePresence, motion } from "framer-motion";
import { AlertTriangle, CheckCircle2, Info, X } from "lucide-react";
import { useEffect } from "react";
import { cn } from "@/lib/cn";
import { useToasts, type Toast } from "@/store/toasts";

export function ToastViewport() {
  const toasts = useToasts((s) => s.toasts);

  return (
    <div
      aria-live="polite"
      aria-atomic="true"
      className="pointer-events-none fixed right-4 top-4 z-[60] flex w-full max-w-sm flex-col gap-2"
    >
      <AnimatePresence initial={false}>
        {toasts.map((t) => (
          <ToastCard key={t.id} toast={t} />
        ))}
      </AnimatePresence>
    </div>
  );
}

function ToastCard({ toast }: { toast: Toast }) {
  const dismiss = useToasts((s) => s.dismiss);

  useEffect(() => {
    if (!toast.duration || toast.duration <= 0) return;
    const id = setTimeout(() => dismiss(toast.id), toast.duration);
    return () => clearTimeout(id);
  }, [toast.id, toast.duration, dismiss]);

  const palette =
    toast.kind === "error"
      ? "border-rose-500/40 bg-rose-500/10 text-rose-100"
      : toast.kind === "success"
      ? "border-emerald-500/40 bg-emerald-500/10 text-emerald-100"
      : "border-white/10 bg-white/[0.06] text-white";

  const Icon =
    toast.kind === "error"
      ? AlertTriangle
      : toast.kind === "success"
      ? CheckCircle2
      : Info;

  return (
    <motion.div
      layout
      initial={{ opacity: 0, x: 24, scale: 0.96 }}
      animate={{ opacity: 1, x: 0, scale: 1 }}
      exit={{ opacity: 0, x: 24, scale: 0.96 }}
      transition={{ duration: 0.22, ease: [0.16, 1, 0.3, 1] }}
      className={cn(
        "pointer-events-auto flex items-start gap-3 rounded-2xl border px-4 py-3 shadow-[0_18px_40px_-15px_rgba(0,0,0,0.7)] backdrop-blur",
        palette
      )}
      role={toast.kind === "error" ? "alert" : "status"}
    >
      <Icon size={16} className="mt-0.5 shrink-0" />
      <div className="min-w-0 flex-1 text-sm leading-relaxed">
        <p className="break-words">{toast.message}</p>
        {toast.action && (
          <button
            onClick={() => {
              toast.action?.onClick();
              dismiss(toast.id);
            }}
            className="mt-1.5 rounded-full border border-white/15 bg-white/[0.06] px-3 py-1 text-xs font-medium text-white transition hover:bg-white/[0.12]"
          >
            {toast.action.label}
          </button>
        )}
      </div>
      <button
        onClick={() => dismiss(toast.id)}
        aria-label="Dismiss"
        className="ml-1 shrink-0 rounded-full p-1 text-white/70 transition hover:bg-white/10 hover:text-white"
      >
        <X size={13} />
      </button>
    </motion.div>
  );
}
