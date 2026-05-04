import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";

export type CapabilityMap = Record<string, { stream: boolean; download: boolean }>;

export function useAddonCapabilities() {
  return useQuery<CapabilityMap>({
    queryKey: ["addons", "capabilities"],
    queryFn: async () => (await api.get<CapabilityMap>("/addons/capabilities")).data,
    staleTime: 30_000,
    refetchOnWindowFocus: false,
    retry: 1,
  });
}

export function useHasStreamAddon(kind: string | undefined) {
  const { data } = useAddonCapabilities();
  if (!kind || !data) return false;
  return !!data[kind.toLowerCase()]?.stream;
}

export function useHasDownloadAddon(kind: string | undefined) {
  const { data } = useAddonCapabilities();
  if (!kind || !data) return false;
  return !!data[kind.toLowerCase()]?.download;
}
