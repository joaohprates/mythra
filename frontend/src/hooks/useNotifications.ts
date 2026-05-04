"use client";

import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/store/auth";
import { api } from "@/lib/api";
import type { Notification } from "@/lib/types";

/**
 * Where to point the SSE EventSource. Goes through the same Next.js dev
 * rewrite that `lib/api.ts` uses, so we never bypass the axios interceptor
 * or hit the wrong port (the backend listens on 5080, not 5000).
 */
const SSE_ORIGIN = process.env.NEXT_PUBLIC_API_ORIGIN ?? "/api";

export function useNotifications() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const qc = useQueryClient();
  const [unreadCount, setUnreadCount] = useState(0);
  const [latest, setLatest] = useState<Notification | null>(null);
  const ctrlRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (!accessToken) return;

    let cancelled = false;

    // Initial unread count fetch — uses the shared axios instance so the
    // request interceptor adds Authorization and the response interceptor
    // can refresh on 401.
    api
      .get<{ count: number }>("/notifications/unread-count")
      .then((r) => {
        if (!cancelled) setUnreadCount(r.data.count);
      })
      .catch(() => {});

    // SSE stream via fetch (EventSource doesn't support custom headers).
    const ctrl = new AbortController();
    ctrlRef.current = ctrl;
    let buffer = "";

    const sseUrl = SSE_ORIGIN.startsWith("http")
      ? `${SSE_ORIGIN}/api/v1/notifications/stream`
      : `${SSE_ORIGIN}/v1/notifications/stream`;

    fetch(sseUrl, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        Accept: "text/event-stream",
      },
      signal: ctrl.signal,
    })
      .then((res) => {
        if (!res.body) return;
        const reader = res.body.getReader();
        const decoder = new TextDecoder();

        const pump = (): Promise<void> =>
          reader.read().then(({ done, value }) => {
            if (done) return;
            buffer += decoder.decode(value, { stream: true });
            const parts = buffer.split("\n\n");
            buffer = parts.pop() ?? "";
            for (const part of parts) {
              const line = part.replace(/^data:\s*/, "").trim();
              if (!line) continue;
              try {
                const n: Notification = JSON.parse(line);
                setLatest(n);
                if (!n.isRead) setUnreadCount((c) => c + 1);
                qc.invalidateQueries({ queryKey: ["notifications"] });
              } catch {
                // heartbeat or malformed — ignore
              }
            }
            return pump();
          });

        pump().catch(() => {});
      })
      .catch(() => {});

    return () => {
      cancelled = true;
      ctrl.abort();
    };
  }, [accessToken, qc]);

  const markAllRead = () => setUnreadCount(0);

  return { unreadCount, latest, markAllRead };
}
