"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { configureApi } from "@/lib/api";
import { tokenStoreAdapter, useAuthStore } from "@/store/auth";

export function Providers({ children }: { children: React.ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            refetchOnWindowFocus: false,
            retry: 1,
          },
        },
      })
  );

  const isHydrated = useAuthStore((s) => s.isHydrated);

  useEffect(() => {
    configureApi(tokenStoreAdapter);
  }, []);

  return (
    <QueryClientProvider client={client}>
      <div data-mythra-hydrated={isHydrated}>{children}</div>
    </QueryClientProvider>
  );
}
