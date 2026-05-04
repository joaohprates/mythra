"use client";

import { useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { WifiOff, X } from "lucide-react";
import { useNetworkHealth } from "@/store/networkHealth";
import { useTranslation } from "@/store/locale";

/**
 * Top banner that appears when the rolling 60-second window of image-load
 * errors crosses the threshold defined in `useNetworkHealth`. Lets the user
 * dismiss it; it'll resurface naturally once the rolling counter rises again
 * after a fresh window of failures.
 */
export function ConnectivityBanner() {
  const t = useTranslation();
  const failureRateHigh = useNetworkHealth((s) => s.failureRateHigh);
  const prune = useNetworkHealth((s) => s.prune);
  const [dismissed, setDismissed] = useState(false);

  // Periodically prune the rolling-window list so failureRateHigh flips back
  // to false even when no new errors arrive.
  useEffect(() => {
    const id = setInterval(() => prune(), 5_000);
    return () => clearInterval(id);
  }, [prune]);

  // Reset the dismissed flag when the failure subsides — that way the banner
  // can reappear later if a new outage starts.
  useEffect(() => {
    if (!failureRateHigh) setDismissed(false);
  }, [failureRateHigh]);

  const visible = failureRateHigh && !dismissed;

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          key="connectivity-banner"
          initial={{ y: -40, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: -40, opacity: 0 }}
          transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
          className="fixed inset-x-0 top-0 z-[60] flex justify-center px-4 pt-3"
          role="status"
          aria-live="polite"
        >
          <div className="flex max-w-3xl items-start gap-3 rounded-2xl border border-amber-400/30 bg-amber-500/10 px-4 py-3 text-amber-100 backdrop-blur shadow-[0_18px_40px_-15px_rgba(251,191,36,0.45)]">
            <WifiOff size={16} className="mt-0.5 shrink-0 text-amber-300" />
            <p className="text-xs leading-relaxed sm:text-sm">
              {t("network.blocked.banner")}
            </p>
            <button
              type="button"
              onClick={() => setDismissed(true)}
              aria-label="Dismiss"
              className="-mr-1 -mt-1 ml-2 grid h-7 w-7 shrink-0 place-items-center rounded-full text-amber-100/70 transition hover:bg-white/10 hover:text-amber-50"
            >
              <X size={14} />
            </button>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
