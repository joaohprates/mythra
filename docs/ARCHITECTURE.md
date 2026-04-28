# Mythra — Architecture

> A self-hosted, multi-media streaming platform with a cinematic frontend.

## High-level shape

```
┌─────────────────────────────────────────────────────────────┐
│                       Mythra Frontend                        │
│   Next.js 15 · React 19 · Tailwind 4 · Framer Motion · hls.js│
└─────────────────────────────────────────────────────────────┘
                          │ HTTPS / WS
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                       Mythra API (ASP.NET Core)              │
│  ┌────────────┐  ┌───────────────┐  ┌──────────────────────┐ │
│  │ Controllers│  │ SyncPlay Hub  │  │ /metrics (Prometheus)│ │
│  └─────┬──────┘  └───────┬───────┘  └──────────────────────┘ │
└────────┼─────────────────┼───────────────────────────────────┘
         ▼                 ▼
┌─────────────────────────────────────────────────────────────┐
│                       Mythra Application                     │
│   Use cases · Services · DTOs · Validators · Ports          │
└────────┬────────────────────────────────────────────────────┘
         ▼
┌─────────────────────────────────────────────────────────────┐
│                       Mythra Infrastructure                  │
│  EF Core │ JWT │ FFmpeg │ Scanners │ Metadata HTTP │ Jobs   │
└────────┬────────────────────────────────────────────────────┘
         ▼
┌─────────────────────────────────────────────────────────────┐
│                       Mythra Domain                          │
│   Entities · Value Objects · Aggregates · Domain events     │
└─────────────────────────────────────────────────────────────┘
```

## Clean architecture layers

- **Domain** — pure C#, no dependencies. Entities, value objects, domain events,
  domain errors. Hierarchy uses TPH discriminator (`MediaItem` →
  `VideoItem`/`MangaItem`/`BookItem`/`AudioItem`).
- **Application** — orchestrates use cases. Services depend only on Domain and
  *ports* (`IMediaProbe`, `ITranscoder`, `IMetadataProvider`,
  `IBackgroundJobQueue`, repositories, …). FluentValidation for inputs.
  `Result<T>` for explicit error flow.
- **Infrastructure** — adapters: EF Core SQLite, BCrypt password hashing, JWT
  token service, FFmpeg-based probe & transcoder, scanners (video, manga, book,
  audio), metadata providers (TMDb, AniList, MusicBrainz, Google Books),
  Channel-backed background queue.
- **API** — ASP.NET Core controllers, custom `ErrorHandlingMiddleware`,
  `CorrelationIdMiddleware`, raw WebSocket `SyncPlayHub`, Swagger UI, JWT auth,
  Serilog request logging, Prometheus `/metrics`.

## Multi-media as first-class

Every library type has its own scanner, its own UX, and inherits from a
shared `MediaItem` aggregate so cross-cutting features (search, recommendations,
progress, recently-added) work uniformly.

| Kind     | Scanner                | Reader / player         | Metadata adapter            |
|----------|------------------------|-------------------------|-----------------------------|
| Video    | `VideoLibraryScanner`  | HLS `<VideoPlayer>`     | TMDb · AniList              |
| Manga    | `MangaLibraryScanner`  | `<MangaReader>` (CBZ)   | AniList                     |
| Book     | `BookLibraryScanner`   | `<BookReader>` (EPUB)   | Google Books                |
| Audio    | `AudioLibraryScanner`  | `<AudioPlayer>` chapters| MusicBrainz                 |

## Streaming pipeline

1. Client calls `POST /api/v1/stream/start` with the video item id.
2. `StreamingService` loads the video, decides **DirectPlay / Remux / Transcode**
   based on codecs and bitrate, creates a `StreamSession`.
3. `FfmpegTranscoder` spawns `ffmpeg` with HLS output to `/tmp/mythra/transcode/<token>/`.
4. `GET /api/v1/stream/{token}/playlist.m3u8` and segment endpoints stream back to hls.js.
5. `DELETE /api/v1/stream/{token}` kills the ffmpeg process and ends the session.

## SyncPlay

Raw WebSocket at `/ws/sync/{code}`. Host-only commands; `Play`, `Pause`,
`Seek`, `ChangeMedia`, `Ready`, `Buffer` are broadcast to peers.
Rooms persisted in SQLite for durability across reconnects.

## Cinematic motion design

`src/lib/motion.ts` defines:

- Duration scale: `instant / fast / medium / slow / cinematic` (80–900 ms).
- Easing curves: `outQuint`, `outExpo`, `inOut`, `spring`.
- Variants: `fadeRise`, `heroBackdrop`, `cardHover`, `stagger`.

CSS layer in `globals.css` exposes Mythra brand tokens
(`--color-mythra-purple/blue/magenta`, `--shadow-glow-purple`, etc.) so every
component pulls from one source of truth.

GPU-friendly transforms (`scale`, `translate`, `opacity`, `filter: blur`) are
preferred over layout-affecting properties.

## Tests

| Project                         | Framework      | Tests |
|---------------------------------|----------------|-------|
| `Mythra.Domain.Tests`           | xUnit v3       | 33    |
| `Mythra.Application.Tests`      | xUnit v3       | 6     |
| `Mythra.Api.Tests`              | xUnit v3 + WAF | 2     |
| `frontend` (Vitest)             | Vitest         | 6     |
| **Total**                       |                | **47**|
