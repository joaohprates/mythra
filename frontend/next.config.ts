import type { NextConfig } from "next";

// API_INTERNAL_ORIGIN  → usado pelo servidor Next.js para fazer proxy (dentro do Docker: http://api:5080)
// NEXT_PUBLIC_API_ORIGIN → usado pelo browser (deixa vazio para usar a URL relativa /api)
const apiOrigin =
  process.env.API_INTERNAL_ORIGIN ||
  process.env.NEXT_PUBLIC_API_ORIGIN ||
  "http://localhost:5080";

const config: NextConfig = {
  reactStrictMode: true,
  poweredByHeader: false,
  output: "standalone",
  experimental: {
    optimizePackageImports: ["lucide-react", "framer-motion"],
  },
  images: {
    remotePatterns: [
      { protocol: "https", hostname: "image.tmdb.org" },
      { protocol: "https", hostname: "s4.anilist.co" },
      { protocol: "https", hostname: "books.google.com" },
      { protocol: "https", hostname: "coverartarchive.org" },
    ],
  },
  async rewrites() {
    return [
      { source: "/api/:path*", destination: `${apiOrigin}/api/:path*` },
      { source: "/ws/:path*", destination: `${apiOrigin}/ws/:path*` },
      { source: "/metrics", destination: `${apiOrigin}/metrics` },
    ];
  },
};

export default config;
