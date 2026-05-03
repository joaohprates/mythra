import { create } from "zustand";
import { persist } from "zustand/middleware";
import { buildTranslator, detectLocale, type Locale, type TranslationKey } from "@/lib/i18n";

interface LocaleState {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: TranslationKey, vars?: Record<string, string>) => string;
}

export const useLocaleStore = create<LocaleState>()(
  persist(
    (set) => ({
      locale: detectLocale(),
      setLocale: (locale) => set({ locale, t: buildTranslator(locale) }),
      t: buildTranslator(detectLocale()),
    }),
    { name: "mythra-locale" }
  )
);

/** Convenience hook — returns the translator function directly. */
export function useTranslation() {
  return useLocaleStore((s) => s.t);
}
