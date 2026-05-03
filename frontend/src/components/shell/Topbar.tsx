"use client";

import { motion, AnimatePresence } from "framer-motion";
import { Bell, Search, Sparkles, Telescope, ListMusic, BarChart2, ShieldAlert, ChevronDown, Heart } from "lucide-react";
import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "@/store/auth";
import { cn } from "@/lib/cn";
import { ProfileBadge } from "./ProfileBadge";
import { useNotifications } from "@/hooks/useNotifications";
import { useLocaleStore, useTranslation } from "@/store/locale";
import { useProfilePrefs } from "@/store/profile";
import { LOCALES, type Locale } from "@/lib/i18n";

export function Topbar() {
  const [scrolled, setScrolled] = useState(false);
  const [langOpen, setLangOpen] = useState(false);
  const langRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 24);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (langRef.current && !langRef.current.contains(e.target as Node)) {
        setLangOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const user = useAuthStore((s) => s.user);
  const activeProfile = useAuthStore((s) => s.activeProfile);
  const { unreadCount } = useNotifications();
  const t = useTranslation();
  const { locale, setLocale } = useLocaleStore();
  const { showAdultContent } = useProfilePrefs();

  const currentLocale = LOCALES.find((l) => l.code === locale) ?? LOCALES[0];

  return (
    <motion.header
      initial={{ opacity: 0, y: -16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.6, ease: [0.16, 1, 0.3, 1] }}
      className={cn(
        "sticky top-0 z-40 transition-[backdrop-filter,background] duration-500",
        scrolled
          ? "bg-[rgba(6,7,13,0.78)] backdrop-blur-xl border-b border-white/[0.06]"
          : "bg-transparent"
      )}
    >
      <div className="mx-auto flex h-16 max-w-[1700px] items-center gap-6 px-6 lg:px-10">
        {/* Logo */}
        <Link href="/" className="flex items-center gap-2 group">
          <motion.span
            whileHover={{ rotate: 12, scale: 1.06 }}
            transition={{ duration: 0.4, ease: [0.34, 1.56, 0.64, 1] }}
            className="grid h-9 w-9 place-items-center rounded-xl bg-gradient-to-br from-[#a855f7] via-[#3b82f6] to-[#ec4899] mythra-glow-purple"
          >
            <Sparkles size={18} className="text-white" />
          </motion.span>
          <span className="text-lg font-semibold tracking-tight">
            <span className="gradient-text">Mythra</span>
          </span>
        </Link>

        {/* Nav */}
        <nav className="hidden items-center gap-1 md:flex">
          <NavLink href="/" label={t("nav.home")} />
          <NavLink href="/library/all/Video" label={t("kind.video")} />
          <NavLink href="/library/all/Manga" label={t("kind.manga")} />
          <NavLink href="/library/all/Book" label={t("kind.book")} />
          <NavLink href="/library/all/Audio" label={t("kind.audio")} />
          <NavLink href="/discover" label={t("nav.discover")} icon={<Telescope size={13} />} highlight />
          <NavLink href="/playlists" label={t("nav.playlists")} icon={<ListMusic size={13} />} />
          <NavLink href="/statistics" label={t("nav.statistics")} icon={<BarChart2 size={13} />} />
          <NavLink href="/favorites" label={t("nav.favorites")} icon={<Heart size={13} />} />
          {showAdultContent && (
            <NavLink
              href="/adult"
              label={t("nav.adult")}
              icon={<ShieldAlert size={13} />}
              adult
            />
          )}
        </nav>

        <div className="ml-auto flex items-center gap-3">
          {/* Language dropdown */}
          <div className="relative" ref={langRef}>
            <button
              onClick={() => setLangOpen((o) => !o)}
              title={t("settings.language")}
              className="flex h-8 items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.03] px-2.5 text-[11px] font-medium text-mythra-text-muted transition hover:bg-white/[0.07] hover:text-white"
            >
              <span>{currentLocale.flag}</span>
              <span>{currentLocale.code.toUpperCase()}</span>
              <ChevronDown size={10} className={cn("transition-transform", langOpen && "rotate-180")} />
            </button>

            <AnimatePresence>
              {langOpen && (
                <motion.div
                  initial={{ opacity: 0, y: -6, scale: 0.96 }}
                  animate={{ opacity: 1, y: 0, scale: 1 }}
                  exit={{ opacity: 0, y: -6, scale: 0.96 }}
                  transition={{ duration: 0.15 }}
                  className="absolute right-0 top-full mt-2 w-40 overflow-hidden rounded-2xl border border-white/10 bg-[#0d0f1c] shadow-2xl"
                >
                  {LOCALES.map((loc) => (
                    <button
                      key={loc.code}
                      onClick={() => { setLocale(loc.code as Locale); setLangOpen(false); }}
                      className={cn(
                        "flex w-full items-center gap-2.5 px-3.5 py-2.5 text-sm transition-colors hover:bg-white/[0.06]",
                        locale === loc.code
                          ? "text-white font-medium"
                          : "text-mythra-text-muted"
                      )}
                    >
                      <span>{loc.flag}</span>
                      <span>{loc.label}</span>
                      {locale === loc.code && (
                        <span className="ml-auto h-1.5 w-1.5 rounded-full bg-mythra-purple" />
                      )}
                    </button>
                  ))}
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Search */}
          <Link
            href="/search"
            className="flex h-10 w-10 items-center justify-center rounded-full text-mythra-text-muted hover:text-white hover:bg-white/5 transition-colors"
            aria-label={t("action.search")}
          >
            <Search size={18} />
          </Link>

          {/* Notifications */}
          <Link
            href="/notifications"
            className="relative flex h-10 w-10 items-center justify-center rounded-full text-mythra-text-muted hover:text-white hover:bg-white/5 transition-colors"
            aria-label="Notifications"
          >
            <Bell size={18} />
            {unreadCount > 0 && (
              <motion.span
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-mythra-blue px-1 text-[9px] font-bold text-white leading-none"
              >
                {unreadCount > 99 ? "99+" : unreadCount}
              </motion.span>
            )}
          </Link>

          {/* Profile / Sign in */}
          {user ? <ProfileBadge /> : (
            <Link
              href="/login"
              className="rounded-full bg-gradient-to-r from-[#a855f7] to-[#3b82f6] px-5 py-2 text-sm font-medium text-white shadow-[0_18px_40px_-15px_rgba(168,85,247,0.7)] hover:shadow-[0_22px_55px_-15px_rgba(168,85,247,0.85)] transition-shadow"
            >
              Sign in
            </Link>
          )}
        </div>
      </div>
    </motion.header>
  );
}

function NavLink({
  href, label, icon, highlight, adult,
}: {
  href: string;
  label: string;
  icon?: React.ReactNode;
  highlight?: boolean;
  adult?: boolean;
}) {
  return (
    <Link
      href={href}
      className={cn(
        "flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium transition-colors relative group",
        highlight && "text-mythra-purple hover:text-white",
        adult && "text-red-400/80 hover:text-red-300",
        !highlight && !adult && "text-mythra-text-muted hover:text-white"
      )}
    >
      {icon}
      <span className="relative z-10">{label}</span>
      <span className="absolute inset-0 rounded-full bg-white/0 group-hover:bg-white/[0.05] transition-colors" />
    </Link>
  );
}
