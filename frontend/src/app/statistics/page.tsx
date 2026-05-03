"use client";

import { useQuery } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { BarChart2, Clock, CheckCircle2, BookOpen, Play, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { useTranslation } from "@/store/locale";
import type { ProfileStatistics } from "@/lib/types";

// ── Helpers ───────────────────────────────────────────────────────────────────

function parseDuration(iso: string): number {
  const m = iso.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/);
  if (!m) return 0;
  return (parseInt(m[1] ?? "0") * 3600) + (parseInt(m[2] ?? "0") * 60) + parseFloat(m[3] ?? "0");
}

function formatHours(iso: string): string {
  const totalSecs = parseDuration(iso);
  const h = Math.floor(totalSecs / 3600);
  const m = Math.floor((totalSecs % 3600) / 60);
  if (h === 0) return `${m}m`;
  return `${h}h ${m}m`;
}

// ── Sub-components ────────────────────────────────────────────────────────────

function StatCard({
  label, value, icon, accent,
}: {
  label: string;
  value: string | number;
  icon: React.ReactNode;
  accent: string;
}) {
  return (
    <motion.div
      whileHover={{ y: -2 }}
      className="rounded-2xl border border-white/[0.06] bg-white/[0.03] p-5 transition-colors hover:border-white/10"
    >
      <div className={`mb-4 grid h-10 w-10 place-items-center rounded-xl ${accent}`}>
        {icon}
      </div>
      <p className="text-2xl font-bold tracking-tight text-white">{value}</p>
      <p className="mt-1 text-xs text-mythra-text-soft">{label}</p>
    </motion.div>
  );
}

function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-white/[0.06] bg-white/[0.02] p-6">
      <h2 className="mb-5 text-sm font-semibold uppercase tracking-widest text-mythra-text-soft">
        {title}
      </h2>
      {children}
    </div>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export default function StatisticsPage() {
  const router = useRouter();
  const { activeProfile, accessToken, isHydrated } = useAuthStore();
  const profileId = activeProfile?.id;
  const t = useTranslation();

  useEffect(() => {
    if (isHydrated && !accessToken) router.replace("/login");
  }, [isHydrated, accessToken, router]);

  const { data: stats, isLoading } = useQuery<ProfileStatistics>({
    queryKey: ["statistics", profileId],
    queryFn: () => api.get(`/profiles/${profileId}/statistics?weeks=12`).then((r) => r.data),
    enabled: !!profileId && !!accessToken,
  });

  return (
    <>
      <Topbar />
      <PageScaffold>
        {/* ── Page header ── */}
        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
        >
          <div className="flex items-center gap-3">
            <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-mythra-purple to-mythra-blue text-white">
              <BarChart2 size={20} />
            </span>
            <div>
              <h1 className="text-3xl font-bold tracking-tight md:text-4xl">
                <span className="gradient-text">{t("stats.title")}</span>
              </h1>
              <p className="text-sm text-mythra-text-muted">
                {t("stats.weeklyActivity")} · 12 semanas
              </p>
            </div>
          </div>
        </motion.div>

        {/* ── No profile ── */}
        {!profileId && (
          <div className="mt-24 flex flex-col items-center gap-3 text-center">
            <BarChart2 size={40} className="text-mythra-text-muted" />
            <p className="text-mythra-text-muted">Selecione um perfil para ver estatísticas.</p>
          </div>
        )}

        {/* ── Loading ── */}
        {profileId && isLoading && (
          <div className="mt-24 flex justify-center">
            <Loader2 size={28} className="animate-spin text-mythra-text-muted" />
          </div>
        )}

        {/* ── Content ── */}
        {stats && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.1 }}
            className="mt-10 space-y-6"
          >
            {/* Summary cards */}
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <StatCard
                label={t("stats.watchTime")}
                value={formatHours(stats.totalWatchTime)}
                icon={<Clock size={18} className="text-blue-400" />}
                accent="bg-blue-500/10"
              />
              <StatCard
                label={t("stats.readTime")}
                value={formatHours(stats.totalReadTime)}
                icon={<BookOpen size={18} className="text-emerald-400" />}
                accent="bg-emerald-500/10"
              />
              <StatCard
                label={t("stats.totalItems")}
                value={stats.totalItemsWatched + stats.totalItemsRead}
                icon={<Play size={18} className="text-mythra-purple" />}
                accent="bg-mythra-purple/10"
              />
              <StatCard
                label={t("stats.completed")}
                value={stats.totalItemsCompleted}
                icon={<CheckCircle2 size={18} className="text-amber-400" />}
                accent="bg-amber-500/10"
              />
            </div>

            <div className="grid gap-6 lg:grid-cols-2">
              {/* Top genres */}
              {stats.topGenres.length > 0 && (
                <SectionCard title={t("stats.topGenres")}>
                  <div className="space-y-4">
                    {stats.topGenres.map((g) => (
                      <div key={g.genre}>
                        <div className="mb-1.5 flex items-center justify-between">
                          <span className="text-sm text-white/80">{g.genre}</span>
                          <span className="text-xs text-mythra-text-soft">
                            {g.count} itens · {g.percentage}%
                          </span>
                        </div>
                        <div className="h-1.5 overflow-hidden rounded-full bg-white/[0.06]">
                          <motion.div
                            initial={{ width: 0 }}
                            animate={{ width: `${g.percentage}%` }}
                            transition={{ duration: 0.8, ease: "easeOut", delay: 0.15 }}
                            className="h-full rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue"
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </SectionCard>
              )}

              {/* Kind breakdown */}
              {stats.kindBreakdown.length > 0 && (
                <SectionCard title={t("stats.kindBreakdown")}>
                  <div className="flex gap-4">
                    {stats.kindBreakdown.map((k, i) => {
                      const colors = [
                        "from-mythra-purple to-mythra-blue",
                        "from-emerald-500 to-teal-500",
                        "from-amber-500 to-orange-500",
                        "from-pink-500 to-rose-500",
                      ];
                      return (
                        <div key={k.kind} className="flex flex-1 flex-col items-center gap-2 text-center">
                          <div className="h-20 w-full overflow-hidden rounded-xl bg-white/[0.05]">
                            <motion.div
                              initial={{ height: 0 }}
                              animate={{ height: `${k.percentage}%` }}
                              transition={{ duration: 0.9, ease: "easeOut", delay: 0.2 + i * 0.05 }}
                              className={`mt-auto w-full rounded-xl bg-gradient-to-t ${colors[i % colors.length]}`}
                              style={{ marginTop: "auto" }}
                            />
                          </div>
                          <p className="text-[11px] text-mythra-text-soft">{k.kind}</p>
                          <p className="text-sm font-semibold text-white">{k.percentage}%</p>
                        </div>
                      );
                    })}
                  </div>
                </SectionCard>
              )}
            </div>

            {/* Weekly activity */}
            {stats.weeklyActivity.length > 0 && (
              <SectionCard title={t("stats.weeklyActivity")}>
                <div className="flex items-end gap-1.5" style={{ height: 100 }}>
                  {stats.weeklyActivity.map((w, i) => {
                    const maxItems = Math.max(...stats.weeklyActivity.map((x) => x.itemsWatched), 1);
                    const pct = w.itemsWatched / maxItems;
                    return (
                      <motion.div
                        key={i}
                        initial={{ height: 0 }}
                        animate={{ height: `${Math.max(pct * 90, 4)}px` }}
                        transition={{ duration: 0.6, ease: "easeOut", delay: i * 0.03 }}
                        className="group relative flex-1 cursor-default rounded-t-lg bg-mythra-purple/30 transition-colors hover:bg-mythra-purple/60"
                        title={`${w.week}: ${w.itemsWatched} itens`}
                      >
                        {w.itemsWatched > 0 && (
                          <span className="absolute -top-6 left-1/2 -translate-x-1/2 whitespace-nowrap rounded bg-black/80 px-1.5 py-0.5 text-[10px] text-white opacity-0 group-hover:opacity-100 transition-opacity">
                            {w.itemsWatched}
                          </span>
                        )}
                      </motion.div>
                    );
                  })}
                </div>
                <div className="mt-3 flex justify-between">
                  <span className="text-[11px] text-mythra-text-soft">{stats.weeklyActivity[0]?.week}</span>
                  <span className="text-[11px] text-mythra-text-soft">{stats.weeklyActivity[stats.weeklyActivity.length - 1]?.week}</span>
                </div>
              </SectionCard>
            )}
          </motion.div>
        )}
      </PageScaffold>
    </>
  );
}
