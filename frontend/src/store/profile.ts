"use client";

import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";

interface ProfilePrefsState {
  showAdultContent: boolean;
  isHydrated: boolean;
  setShowAdultContent: (value: boolean) => void;
  markHydrated: () => void;
}

export const useProfilePrefs = create<ProfilePrefsState>()(
  persist(
    (set) => ({
      showAdultContent: false,
      isHydrated: false,
      setShowAdultContent: (value) => set({ showAdultContent: value }),
      markHydrated: () => set({ isHydrated: true }),
    }),
    {
      name: "mythra-profile-prefs",
      storage: createJSONStorage(() => {
        if (typeof window === "undefined") {
          return {
            getItem: () => null,
            setItem: () => {},
            removeItem: () => {},
          };
        }
        return localStorage;
      }),
      partialize: (state) => ({ showAdultContent: state.showAdultContent }),
      onRehydrateStorage: () => (state) => {
        state?.markHydrated();
      },
    }
  )
);
