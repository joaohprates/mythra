"use client";

import { QueryClient, QueryClientProvider, MutationCache, QueryCache } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { configureApi } from "@/lib/api";
import { tokenStoreAdapter, useAuthStore } from "@/store/auth";
import { ErrorBoundary } from "./ErrorBoundary";

/**
 * Centralised React Query client. Caches are shared between mounts so an
 * error in one query doesn't poison another route's data.
 *
 * The error caches are intentionally permissive — failures bubble up as
 * `query.error` (rendered inline by each page) rather than throwing into the
 * route boundary. This keeps the user inside the app even when one provider
 * (e.g. Cinemeta) is down.
 */
function makeQueryClient() {
  return new QueryClient({
    queryCache: new QueryCache({
      onError: (error, query) => {
        if (process.env.NODE_ENV !== "production") {
          // eslint-disable-next-line no-console
          console.warn("[mythra] query failed", { key: query.queryKey, error });
        }
      },
    }),
    mutationCache: new MutationCache({
      onError: (error) => {
        if (process.env.NODE_ENV !== "production") {
          // eslint-disable-next-line no-console
          console.warn("[mythra] mutation failed", error);
        }
      },
    }),
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Don't retry on 4xx — they're user/auth errors, not flakiness.
          const status = (error as { response?: { status?: number } })?.response?.status;
          if (status && status >= 400 && status < 500) return false;
          return failureCount < 1;
        },
        // throwOnError defaults to false — keeps errors out of the boundary.
      },
      mutations: {
        retry: 0,
      },
    },
  });
}

export function Providers({ children }: { children: React.ReactNode }) {
  const [client] = useState(makeQueryClient);
  const isHydrated = useAuthStore((s) => s.isHydrated);

  useEffect(() => {
    configureApi(tokenStoreAdapter);
  }, []);

  return (
    <ErrorBoundary>
      <QueryClientProvider client={client}>
        <div data-mythra-hydrated={isHydrated}>{children}</div>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
