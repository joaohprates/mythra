"use client";

import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import type { User, Profile, AuthResponse } from "@/lib/types";

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  accessExpiresAt: string | null;
  refreshExpiresAt: string | null;
  activeProfile: Profile | null;
  isHydrated: boolean;

  setAuth(payload: AuthResponse): void;
  setTokens(access: string, refresh: string, accessExpiresAt: string, refreshExpiresAt: string): void;
  setUser(user: User): void;
  setActiveProfile(profile: Profile | null): void;
  clear(): void;
  markHydrated(): void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      accessExpiresAt: null,
      refreshExpiresAt: null,
      activeProfile: null,
      isHydrated: false,

      setAuth: (payload) =>
        set({
          user: payload.user,
          accessToken: payload.accessToken,
          refreshToken: payload.refreshToken,
          accessExpiresAt: payload.accessExpiresAt,
          refreshExpiresAt: payload.refreshExpiresAt,
          activeProfile: payload.user.profiles[0] ?? null,
        }),

      setTokens: (access, refresh, accessExpiresAt, refreshExpiresAt) =>
        set({ accessToken: access, refreshToken: refresh, accessExpiresAt, refreshExpiresAt }),

      setUser: (user) => set({ user }),

      setActiveProfile: (profile) => set({ activeProfile: profile }),

      clear: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          accessExpiresAt: null,
          refreshExpiresAt: null,
          activeProfile: null,
        }),

      markHydrated: () => set({ isHydrated: true }),
    }),
    {
      name: "mythra-auth",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        accessExpiresAt: state.accessExpiresAt,
        refreshExpiresAt: state.refreshExpiresAt,
        activeProfile: state.activeProfile,
      }),
      onRehydrateStorage: () => (state) => {
        state?.markHydrated();
      },
    }
  )
);

export const tokenStoreAdapter = {
  getAccessToken: () => useAuthStore.getState().accessToken,
  getRefreshToken: () => useAuthStore.getState().refreshToken,
  setTokens: (access: string, refresh: string, accessExpiresAt: string, refreshExpiresAt: string) =>
    useAuthStore.getState().setTokens(access, refresh, accessExpiresAt, refreshExpiresAt),
  clear: () => useAuthStore.getState().clear(),
};
