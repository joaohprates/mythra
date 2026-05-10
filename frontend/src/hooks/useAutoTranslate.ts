"use client";

import { useEffect, useState } from "react";
import { useAuthStore } from "@/store/auth";

/**
 * Automatically translates `text` to the user's preferred language using the
 * Google Translate public endpoint (no API key required).
 *
 * - Returns the original text immediately while translation is in-flight.
 * - Falls back to original text silently on any network/parse error.
 * - No-ops when the user's language is English or text is empty.
 */
export function useAutoTranslate(text: string | null | undefined): string {
  const preferredLanguage = useAuthStore((s) => s.user?.preferredLanguage ?? "en");
  // Normalise "pt-BR" → "pt", "zh-Hant" → "zh-TW", etc.
  const lang = preferredLanguage.split("-")[0].toLowerCase();

  const [translated, setTranslated] = useState<string | null>(null);
  const [lastText, setLastText] = useState<string | null>(null);

  useEffect(() => {
    const cleaned = text?.trim();
    if (!cleaned || lang === "en") {
      setTranslated(null);
      setLastText(null);
      return;
    }
    // Skip if same text already translated
    if (cleaned === lastText) return;

    const controller = new AbortController();
    const url =
      `https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=${encodeURIComponent(lang)}&dt=t&q=${encodeURIComponent(cleaned)}`;

    fetch(url, { signal: controller.signal })
      .then((r) => r.json())
      .then((data: unknown) => {
        // Response: [[["translated chunk", "original",...], ...], null, "detected-lang"]
        if (!Array.isArray(data) || !Array.isArray(data[0])) return;
        const result = (data[0] as Array<[string]>)
          .map((chunk) => chunk[0] ?? "")
          .join("");
        if (result) {
          setTranslated(result);
          setLastText(cleaned);
        }
      })
      .catch(() => {
        /* silently fall back to original */
      });

    return () => controller.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [text, lang]);

  return translated ?? text ?? "";
}
