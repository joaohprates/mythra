"use client";

import { useState, useCallback, type ImgHTMLAttributes, type SyntheticEvent } from "react";
import { proxyImage } from "@/lib/image";
import { useNetworkHealth } from "@/store/networkHealth";

type FallbackKind = "poster" | "backdrop" | "avatar";

export interface SmartImageProps extends ImgHTMLAttributes<HTMLImageElement> {
  fallbackKind?: FallbackKind;
}

/**
 * Resilient <img> wrapper.
 *
 *  - Routes allow-listed external hosts through the backend image proxy
 *    (`/api/v1/proxy/image?url=...`) so the user's restricted ISP can't
 *    block posters at the network layer.
 *  - On error, swaps in a built-in inline-SVG placeholder (purple/magenta
 *    gradient with a Mythra "M" or film glyph) so we never render a broken
 *    image icon. The placeholder is a data URI — no network request.
 *  - Pings the global `useNetworkHealth` store so the connectivity banner
 *    can warn the user when too many images fail in a short window.
 */
export function SmartImage({
  src,
  fallbackKind = "poster",
  onError,
  loading = "lazy",
  decoding = "async",
  referrerPolicy = "no-referrer",
  alt = "",
  ...rest
}: SmartImageProps) {
  const [errored, setErrored] = useState(false);

  const handleError = useCallback(
    (event: SyntheticEvent<HTMLImageElement, Event>) => {
      if (!errored) {
        setErrored(true);
        // Best-effort: log to network health store. Guard against SSR / unit-test envs.
        try {
          useNetworkHealth.getState().imageError();
        } catch {
          /* no-op */
        }
      }
      onError?.(event);
    },
    [errored, onError]
  );

  const resolved = errored ? PLACEHOLDER[fallbackKind] : proxyImage(src as string | null | undefined);

  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      {...rest}
      src={resolved || PLACEHOLDER[fallbackKind]}
      alt={alt}
      loading={loading}
      decoding={decoding}
      referrerPolicy={referrerPolicy}
      onError={handleError}
    />
  );
}

/* ── Inline-SVG placeholders ─────────────────────────────────────────────── */

function makePoster(width: number, height: number, glyph: string): string {
  const svg = `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" preserveAspectRatio="xMidYMid slice">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#7c3aed"/>
      <stop offset="55%" stop-color="#4f46e5"/>
      <stop offset="100%" stop-color="#1e1b4b"/>
    </linearGradient>
  </defs>
  <rect width="${width}" height="${height}" fill="url(#g)"/>
  <text x="50%" y="50%" font-family="Plus Jakarta Sans, system-ui, sans-serif"
        font-size="${Math.round(Math.min(width, height) * 0.42)}"
        font-weight="800" fill="rgba(255,255,255,0.85)" text-anchor="middle"
        dominant-baseline="central" letter-spacing="-2">${glyph}</text>
</svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

const PLACEHOLDER: Record<FallbackKind, string> = {
  poster: makePoster(400, 600, "M"),
  backdrop: makePoster(1280, 720, "M"),
  avatar: makePoster(160, 160, "M"),
};
