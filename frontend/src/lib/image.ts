/**
 * Mythra image proxy helper.
 *
 * The user is on a censored/restricted ISP. Several poster/cover CDNs are
 * blocked at the network level. To stay resilient we route those external
 * URLs through the backend proxy at `/api/v1/proxy/image?url={absoluteUrl}`,
 * which fetches the image server-side and streams it back.
 *
 * Hosts not in the allow-list are returned unchanged (e.g. local URLs,
 * data URIs, already-proxied paths).
 */

const ALLOWED = [
  "image.tmdb.org",
  "s4.anilist.co",
  "books.google.com",
  "coverartarchive.org",
  "covers.openlibrary.org",
  "media.kitsu.io",
  "cdn.myanimelist.net",
  "static.tvmaze.com",
];

export function proxyImage(url: string | null | undefined): string {
  if (!url) return "";
  try {
    const u = new URL(url);
    if (!ALLOWED.includes(u.hostname)) return url;
    return `/api/v1/proxy/image?url=${encodeURIComponent(url)}`;
  } catch {
    return url ?? "";
  }
}
