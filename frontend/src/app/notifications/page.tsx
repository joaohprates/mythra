"use client";

import { motion, AnimatePresence } from "framer-motion";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  Bell, BookOpen, CheckCheck, Film, RefreshCw, Shield, Star, Trash2, Zap,
} from "lucide-react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import type { Notification, NotificationKind } from "@/lib/types";

const KIND_ICON: Record<NotificationKind, React.ReactNode> = {
  MediaAdded:       <Film size={14} />,
  ScanCompleted:    <RefreshCw size={14} />,
  ScanFailed:       <Shield size={14} />,
  Recommendation:   <Star size={14} />,
  ImportCompleted:  <BookOpen size={14} />,
  ProviderUnhealthy:<Zap size={14} />,
  System:           <Bell size={14} />,
};

const KIND_COLOR: Record<NotificationKind, string> = {
  MediaAdded:       "from-mythra-blue to-cyan-500",
  ScanCompleted:    "from-emerald-500 to-teal-500",
  ScanFailed:       "from-rose-500 to-orange-500",
  Recommendation:   "from-mythra-purple to-pink-500",
  ImportCompleted:  "from-mythra-blue to-mythra-purple",
  ProviderUnhealthy:"from-amber-500 to-orange-500",
  System:           "from-slate-500 to-slate-400",
};

export default function NotificationsPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const qc = useQueryClient();
  const t = useTranslation();

  const { data, isLoading } = useQuery({
    queryKey: ["notifications"],
    queryFn: async () => (await api.get<{ items: Notification[]; unreadCount: number }>("/notifications")).data,
    enabled: !!accessToken,
    refetchInterval: 30_000,
  });

  useEffect(() => {
    if (!accessToken) router.replace("/login");
  }, [accessToken, router]);

  if (!accessToken) return null;

  const notifications = data?.items ?? [];

  const markRead = async (id: string) => {
    await api.patch(`/notifications/${id}/read`);
    qc.invalidateQueries({ queryKey: ["notifications"] });
  };

  const markAllRead = async () => {
    await api.patch("/notifications/read-all");
    qc.invalidateQueries({ queryKey: ["notifications"] });
  };

  const deleteNotification = async (id: string) => {
    await api.delete(`/notifications/${id}`);
    qc.invalidateQueries({ queryKey: ["notifications"] });
  };

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex items-start justify-between"
        >
          <div>
            <h1 className="mb-1 text-3xl font-bold tracking-tight md:text-4xl">
              <span className="gradient-text">{t("notifications.title")}</span>
            </h1>
            {data && <p className="text-sm text-mythra-text-muted">{t("notifications.unread", { count: String(data.unreadCount) })}</p>}
          </div>
          {notifications.some((n) => !n.isRead) && (
            <button
              onClick={markAllRead}
              className="mt-1 inline-flex items-center gap-1.5 rounded-full border border-white/10 px-4 py-2 text-xs hover:bg-white/5"
            >
              <CheckCheck size={13} /> {t("notifications.markAllRead")}
            </button>
          )}
        </motion.div>

        {isLoading && (
          <div className="mt-16 flex justify-center">
            <RefreshCw size={20} className="animate-spin text-mythra-text-muted" />
          </div>
        )}

        {!isLoading && notifications.length === 0 && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="mt-24 flex flex-col items-center gap-3 text-center"
          >
            <span className="grid h-16 w-16 place-items-center rounded-full bg-white/[0.04]">
              <Bell size={28} className="text-mythra-text-muted" />
            </span>
            <p className="text-mythra-text-muted">{t("notifications.empty")}</p>
          </motion.div>
        )}

        <ul className="mt-8 space-y-2">
          <AnimatePresence initial={false}>
            {notifications.map((n) => (
              <motion.li
                key={n.id}
                layout
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, height: 0, marginBottom: 0 }}
                className={
                  "group flex items-start gap-3 rounded-2xl border p-4 transition-colors " +
                  (n.isRead
                    ? "border-white/[0.04] bg-white/[0.01]"
                    : "border-white/[0.07] bg-white/[0.04]")
                }
              >
                {/* Icon */}
                <span
                  className={`mt-0.5 grid h-9 w-9 shrink-0 place-items-center rounded-full bg-gradient-to-br ${KIND_COLOR[n.kind]} text-white`}
                >
                  {KIND_ICON[n.kind]}
                </span>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <p className={`text-sm font-medium ${n.isRead ? "text-mythra-text-soft" : "text-white"}`}>
                    {n.title}
                    {!n.isRead && (
                      <span className="ml-2 inline-block h-1.5 w-1.5 rounded-full bg-mythra-blue align-middle" />
                    )}
                  </p>
                  {n.body && <p className="mt-0.5 text-xs text-mythra-text-muted">{n.body}</p>}
                  <p className="mt-1 text-[10px] text-mythra-text-muted">
                    {new Date(n.createdAt).toLocaleString()}
                  </p>
                </div>

                {/* Actions */}
                <div className="flex shrink-0 items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                  {!n.isRead && (
                    <button
                      onClick={() => markRead(n.id)}
                      className="rounded-full p-1.5 hover:bg-white/10"
                      title={t("notifications.markRead")}
                    >
                      <CheckCheck size={13} />
                    </button>
                  )}
                  <button
                    onClick={() => deleteNotification(n.id)}
                    className="rounded-full p-1.5 hover:bg-rose-500/10 text-rose-400"
                    title={t("notifications.delete")}
                  >
                    <Trash2 size={13} />
                  </button>
                </div>
              </motion.li>
            ))}
          </AnimatePresence>
        </ul>
      </PageScaffold>
    </>
  );
}
