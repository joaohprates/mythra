"use client";

import { useEffect } from "react";
import { AlertTriangle, RefreshCw, Home } from "lucide-react";
import Link from "next/link";

export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    if (typeof console !== "undefined") {
      // Surface the error in dev — keep this even in prod since it's only the
      // boundary path (the app already crashed; logging helps the user share repro).
      // eslint-disable-next-line no-console
      console.error("[mythra] route error", error);
    }
  }, [error]);

  return (
    <main className="grid min-h-[80vh] place-items-center px-6">
      <div className="max-w-md text-center">
        <div className="mx-auto mb-6 grid h-20 w-20 place-items-center rounded-full border border-red-500/20 bg-red-500/10 text-red-400">
          <AlertTriangle size={36} />
        </div>
        <h1 className="text-2xl font-bold text-white">Something went wrong</h1>
        <p className="mt-3 text-sm text-mythra-text-muted">
          An unexpected error interrupted this page. The app stays running — pick a path back below.
        </p>
        {error?.digest && (
          <p className="mt-3 text-[11px] text-mythra-text-soft font-mono">ref: {error.digest}</p>
        )}
        <div className="mt-7 flex flex-wrap items-center justify-center gap-3">
          <button
            onClick={reset}
            className="inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2.5 text-sm font-medium text-white"
          >
            <RefreshCw size={14} /> Try again
          </button>
          <Link
            href="/"
            className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-5 py-2.5 text-sm font-medium text-white hover:bg-white/[0.08]"
          >
            <Home size={14} /> Home
          </Link>
        </div>
      </div>
    </main>
  );
}
