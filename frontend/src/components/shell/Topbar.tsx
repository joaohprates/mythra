"use client";

import { motion } from "framer-motion";
import { Bell, Search, Sparkles, Telescope } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { useAuthStore } from "@/store/auth";
import { cn } from "@/lib/cn";
import { ProfileBadge } from "./ProfileBadge";
import { useNotifications } from "@/hooks/useNotifications";

export function Topbar() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 24);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  const user = useAuthStore((s) => s.user);
  const { unreadCount } = useNotifications();

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

        <nav className="hidden items-center gap-1 md:flex">
          <NavLink href="/" label="Home" />
          <NavLink href="/library/all/Video" label="Movies & TV" />
          <NavLink href="/library/all/Manga" label="Manga" />
          <NavLink href="/library/all/Book" label="Books" />
          <NavLink href="/library/all/Audio" label="Audiobooks" />
          <NavLink href="/discover" label="Discover" icon={<Telescope size={13} />} highlight />
        </nav>

        <div className="ml-auto flex items-center gap-3">
          <Link
            href="/search"
            className="flex h-10 w-10 items-center justify-center rounded-full text-mythra-text-muted hover:text-white hover:bg-white/5 transition-colors"
            aria-label="Search"
          >
            <Search size={18} />
          </Link>

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
  href, label, icon, highlight,
}: {
  href: string;
  label: string;
  icon?: React.ReactNode;
  highlight?: boolean;
}) {
  return (
    <Link
      href={href}
      className={cn(
        "flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium transition-colors relative group",
        highlight
          ? "text-mythra-purple hover:text-white"
          : "text-mythra-text-muted hover:text-white"
      )}
    >
      {icon}
      <span className="relative z-10">{label}</span>
      <span className="absolute inset-0 rounded-full bg-white/0 group-hover:bg-white/[0.05] transition-colors" />
    </Link>
  );
}
