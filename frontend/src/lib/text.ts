/**
 * Normalize free-text descriptions that arrive from upstream metadata providers.
 *
 * Real-world payloads from TMDb, AniList, OMDb, Cinemeta, etc. mix three
 * different "line break" representations into the same string:
 *   - HTML tags: `<br>`, `<br/>`, `<br />`
 *   - Escaped sequences: `\n`, `\\n`
 *   - Stripped HTML residue: `/br`, `/br>`, `/p` (when only the leading `<` was removed upstream)
 *
 * If we render the raw string, those tokens leak into the UI as visible junk
 * (the `/br` bug). This helper collapses every variant to a real newline so a
 * downstream `whitespace-pre-line` CSS rule can render paragraphs correctly.
 */
export function cleanDescription(raw: string | null | undefined): string {
  if (!raw) return "";
  return raw
    // Real HTML break tags (allow attributes, allow trailing space before `>`)
    .replace(/<br\b[^>]*>/gi, "\n")
    // Closing paragraph tags, with or without leading `<`
    .replace(/<\/?p\s*>/gi, "\n")
    // Stripped-HTML residue: a stray `/br>` or `/br` left over by a sloppy sanitizer
    .replace(/\/br\s*>?/gi, "\n")
    // Escaped newlines inside JSON strings
    .replace(/\\n/g, "\n")
    .replace(/\\r/g, "")
    // Decode common HTML entities that creep in (TMDb is fond of `&amp;`)
    .replace(/&amp;/g, "&")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&nbsp;/g, " ")
    // Collapse 3+ consecutive newlines into 2 (keeps paragraph breaks but kills runs)
    .replace(/\n{3,}/g, "\n\n")
    .trim();
}

/**
 * Convert a byte count to a human-readable size string.
 * Returns an em-dash for nullish/non-finite values so callers can render the
 * result directly without ternaries.
 */
export function formatBytes(bytes: number | null | undefined): string {
  if (bytes == null || !Number.isFinite(bytes) || bytes <= 0) return "—";
  const units = ["B", "KB", "MB", "GB", "TB"];
  let value = bytes;
  let i = 0;
  while (value >= 1024 && i < units.length - 1) {
    value /= 1024;
    i += 1;
  }
  const decimals = i >= 2 ? (value < 10 ? 2 : 1) : 0;
  return `${value.toFixed(decimals)} ${units[i]}`;
}

