"use client";

import { motion } from "framer-motion";
import { ShieldAlert } from "lucide-react";
import { fadeRise } from "@/lib/motion";
import { cn } from "@/lib/cn";
import { useProfilePrefs } from "@/store/profile";
import { useTranslation } from "@/store/locale";
import { ConnectivityBanner } from "@/components/shell/ConnectivityBanner";
import { ToastViewport } from "@/components/ui/ToastViewport";

export function PageScaffold({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <motion.main
      initial="hidden"
      animate="visible"
      exit="exit"
      variants={fadeRise}
      className={cn("relative mx-auto max-w-[1700px] px-6 pb-24 pt-8 lg:px-10", className)}
    >
      <ConnectivityBanner />
      {children}
      <AdultFloatingButton />
      <ToastViewport />
    </motion.main>
  );
}

function AdultFloatingButton() {
  const { showAdultContent, setShowAdultContent, isHydrated } = useProfilePrefs();
  const t = useTranslation();
  if (!isHydrated) return null;
  const label = showAdultContent ? t("discover.adult.toggle.off") : t("discover.adult.toggle.on");
  return (
    <button
      onClick={() => setShowAdultContent(!showAdultContent)}
      title={label}
      aria-label={label}
      aria-pressed={showAdultContent}
      className={cn(
        "fixed bottom-6 right-6 z-50 grid h-14 w-14 place-items-center rounded-full",
        "bg-gradient-to-br from-mythra-purple via-[#7c3aed] to-mythra-magenta text-white",
        "shadow-[0_18px_40px_-12px_rgba(168,85,247,0.85)] transition-all hover:scale-105",
        showAdultContent
          ? "ring-2 ring-red-400/60 shadow-[0_18px_40px_-12px_rgba(239,68,68,0.7)]"
          : "ring-1 ring-white/10"
      )}
    >
      <ShieldAlert size={22} />
      {showAdultContent && (
        <span className="absolute -top-1 -right-1 grid h-5 w-5 place-items-center rounded-full bg-red-500 text-[9px] font-bold">
          18
        </span>
      )}
    </button>
  );
}
