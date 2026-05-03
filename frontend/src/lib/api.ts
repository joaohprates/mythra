import axios, { type AxiosError, type AxiosInstance } from "axios";

const API_ORIGIN = process.env.NEXT_PUBLIC_API_ORIGIN || "/api";
const isAbsolute = API_ORIGIN.startsWith("http");

export interface TokenStore {
  getAccessToken(): string | null;
  getRefreshToken(): string | null;
  setTokens(access: string, refresh: string, accessExpiresAt: string, refreshExpiresAt: string): void;
  clear(): void;
}

let tokenStore: TokenStore | null = null;

export function configureApi(store: TokenStore) {
  tokenStore = store;
}

export const api: AxiosInstance = axios.create({
  baseURL: isAbsolute ? `${API_ORIGIN}/api/v1` : "/api/v1",
  timeout: 30_000,
  headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
  const token = tokenStore?.getAccessToken();
  if (token) {
    config.headers.set("Authorization", `Bearer ${token}`);
  }
  return config;
});

let refreshing: Promise<string> | null = null;

api.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const original = error.config as (typeof error.config & { __retry?: boolean }) | undefined;
    const refresh = tokenStore?.getRefreshToken();

    // Don't attempt refresh for auth endpoints themselves
    const isAuthEndpoint = original?.url?.includes("/auth/");

    if (status === 401 && refresh && original && !original.__retry && !isAuthEndpoint) {
      original.__retry = true;
      try {
        const newToken = await (refreshing ??= refreshToken(refresh).finally(() => (refreshing = null)));
        original.headers!.Authorization = `Bearer ${newToken}`;
        return api.request(original);
      } catch {
        tokenStore?.clear();
        if (typeof window !== "undefined") window.location.href = "/login";
        return Promise.reject(error);
      }
    }

    // For 5xx errors, attach a human-readable message
    if (status && status >= 500) {
      (error as AxiosError & { userMessage?: string }).userMessage =
        "Server error. Please try again in a moment.";
    }

    return Promise.reject(error);
  }
);

async function refreshToken(refresh: string): Promise<string> {
  const response = await axios.post(
    `${api.defaults.baseURL}/auth/refresh`,
    { refreshToken: refresh },
    { headers: { "Content-Type": "application/json" } }
  );
  const { accessToken, refreshToken: newRefresh, accessExpiresAt, refreshExpiresAt } = response.data;
  tokenStore?.setTokens(accessToken, newRefresh, accessExpiresAt, refreshExpiresAt);
  return accessToken;
}

export function streamUrl(token: string, file = "playlist.m3u8"): string {
  const base = isAbsolute ? API_ORIGIN : "";
  return `${base}/api/v1/stream/${encodeURIComponent(token)}/${file}`;
}
