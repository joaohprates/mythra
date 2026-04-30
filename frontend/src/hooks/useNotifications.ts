"use client";

import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/store/auth";
import type { Notification } from "@/lib/types";

const BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export function useNotifications() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const qc = useQueryClient();
  const [unreadCount, setUnreadCount] = useState(0);
  const [latest, setLatest] = useState<Notification | null>(null);
  const esRef = useRef<EventSource | null>(null);

  useEffect(() => {
    if (!accessToken) return;

    // Initial unread count fetch
    fetch(`${BASE}/api/v1/notifications/unread-count`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    })
      .then((r) => r.json())
      .then((d: { count: number }) => setUnreadCount(d.count))
      .catch(() => {});

    // SSE stream
    const url = new URL(`${BASE}/api/v1/notifications/stream`);
    const es = new EventSource(url.toString(), {
      // EventSource doesn't support custom headers natively;
      // we rely on the server to accept a token query param as fallback.
    });

    // Use native fetch-based SSE via ReadableStream for auth header support
    const ctrl = new AbortController();
    let buffer = "";

    fetch(`${BASE}/api/v1/notifications/stream`, {
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

    esRef.current = es;
    return () => {
      ctrl.abort();
      es.close();
    };
  }, [accessToken, qc]);

  const markAllRead = () => setUnreadCount(0);

  return { unreadCount, latest, markAllRead };
}
