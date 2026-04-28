"use client";

import { motion } from "framer-motion";
import { LogOut, User2 } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { useAuthStore } from "@/store/auth";

export function ProfileBadge() {
  const [open, setOpen] = useState(false);
  const user = useAuthStore((s) => s.user);
  const profile = useAuthStore((s) => s.activeProfile);
  const clear = useAuthStore((s) => s.clear);

  if (!user) return null;
  const initial = (profile?.name ?? user.username).charAt(0).toUpperCase();

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 rounded-full pl-2 pr-3 py-1.5 hover:bg-white/[0.06] transition-colors"
      >
        <span className="grid h-8 w-8 place-items-center rounded-full bg-gradient-to-br from-[#a855f7] via-[#3b82f6] to-[#ec4899] text-sm font-semibold text-white">
          {initial}
        </span>
        <span className="hidden text-sm text-mythra-text-muted md:block">{profile?.name ?? user.username}</span>
      </button>

      {open && (
        <motion.div
          initial={{ opacity: 0, y: -6, scale: 0.97 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          transition={{ duration: 0.18, ease: [0.16, 1, 0.3, 1] }}
          className="absolute right-0 mt-2 w-60 rounded-2xl border border-white/[0.08] bg-[#0c0e1a]/95 backdrop-blur-xl p-2 shadow-mythra-card"
          onMouseLeave={() => setOpen(false)}
        >
          <div className="px-3 py-2 text-xs uppercase tracking-wider text-mythra-text-soft">{user.email}</div>
          <Link
            href="/settings"
            onClick={() => setOpen(false)}
            className="flex items-center gap-2 rounded-xl px-3 py-2 text-sm hover:bg-white/[0.06]"
          >
            <User2 size={16} /> Settings
          </Link>
          <button
            onClick={() => {
              clear();
              setOpen(false);
            }}
            className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm text-rose-300 hover:bg-rose-500/10"
          >
            <LogOut size={16} /> Sign out
          </button>
        </motion.div>
      )}
    </div>
  );
}
