# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Vision

Mythra is a **self-hosted, multi-media streaming platform** — Jellyfin-class depth with Netflix-class polish. The core idea: every media type (video, manga, books, audiobooks) is a **first-class citizen**, not a plugin. Each has its own ingestion pipeline, metadata provider, reader/player UI, and progress system, all sharing cross-cutting infrastructure (search, recommendations, notifications, SyncPlay).

**Brand:** cinematic, dark, immersive. UI uses purple/blue/magenta gradients, glow shadows, and motion as a design system (not decoration).

---

## Commands

All backend commands run from `backend/`. The solution uses the modern `.slnx` format.

```bash
# Build
dotnet build Mythra.slnx

# Run all tests
dotnet test Mythra.slnx

# Run a single test project
dotnet test tests/Mythra.Domain.Tests

# Run tests by name filter
dotnet test Mythra.slnx --filter "FullyQualifiedName~LibraryTests"

# Run the API (SQLite auto-created on first run)
dotnet run --project src/Mythra.Api
# → http://localhost:5080
# → http://localhost:5080/swagger  (dev only)
# → http://localhost:5080/metrics  (Prometheus)
# → http://localhost:5080/api/v1/health

# Frontend
cd ../frontend && npm install && npm run dev
# → http://localhost:3000

# Docker (full stack)
export MYTHRA_JWT_SECRET="$(openssl rand -base64 48)"
docker compose up -d
```

The first registered user is automatically promoted to `Admin`.

---

## Backend Architecture

### Layer Map

```
Mythra.Api             → Controllers, Middleware, SyncPlay WebSocket hub, Program.cs
Mythra.Application     → Service interfaces, repository ports, DTOs, validators
Mythra.Domain          → Entities, aggregates, value objects, domain events (no deps)
Mythra.Infrastructure  → EF Core/SQLite, FFmpeg, metadata providers, scanners, addons
Mythra.Addons.Contracts → Thin SDK for third-party addon DLLs (no ASP.NET Core deps)
```

Dependency direction: `Api → Application ← Infrastructure`. Domain has zero outward dependencies. Each layer has a `DependencyInjection.cs` with an `AddApplication()` / `AddInfrastructure()` extension; `Program.cs` calls both.

### Domain Patterns

**`Result<T>`** — all business logic uses railway-oriented programming. Never throw for expected failures. Return `Result.Success(value)` or `Result.Failure(error)`. Use `ResultExtensions` in the API layer to map to `ActionResult`.

**`AggregateRoot`** — manages a private `_events` list. Call `Raise(domainEvent)` inside aggregate methods. Infrastructure is responsible for dispatching events. Entity equality is by `(Id, Type)`.

**Media hierarchy (TPH):** `MediaItem` (abstract aggregate root) → `VideoItem` / `MangaItem` / `BookItem` / `AudioItem`. EF Core uses a discriminator column. Use `IMediaRepository` for base queries; `IVideoRepository`, `IMangaRepository` etc. for type-specific queries.

**Libraries:** A `Library` aggregate owns `LibraryFolder` value objects and carries `LibraryKind` (Video, Anime, Manga, Book, Audiobook, Music, General, Image). The system auto-provisions a `General` library on first boot (`IsSystem = true`, non-deletable, auto-detects file type by extension). Libraries with `AllowedExtensions = []` inherit the kind's default extension set.

### Media Ingestion Pipeline

```
File on disk / external import
  ↓
ScanService enqueues job → ChannelBackgroundJobQueue
  ↓
Scanner (VideoLibraryScanner / BookLibraryScanner / MangaLibraryScanner / AudioLibraryScanner)
  ↓
FfmpegMediaProbe — extracts codec, resolution, duration, audio tracks, subtitle tracks
  ↓
MetadataProviderRegistry — picks provider by MediaKind
  TMDb (video) · AniList (anime/manga) · GoogleBooks (books) · MusicBrainz (audio)
  ↓
MediaItem saved → domain event MediaItemAdded fired → NotificationService push
  ↓
ScanResult { Added, Updated, Removed, Failed, Elapsed }
```

For **external imports** (Discover → "Add to Library"): item is created with `FilePath = null` and `FileStatus = ExternalOnly`. Stream resolution happens lazily at playback time via the external provider chain.

### Streaming Pipeline

```
POST /api/v1/stream/start  →  StreamingService decides DirectPlay / Remux / Transcode
  ↓
FfmpegTranscoder spawns ffmpeg → HLS segments in /tmp/mythra/transcode/<token>/
  ↓
GET /api/v1/stream/{token}/playlist.m3u8  →  hls.js in browser
  ↓
DELETE /api/v1/stream/{token}  →  kills ffmpeg process, ends StreamSession
```

For external content: `GET /api/v1/stream/external/{id}` calls `ExternalProviderService`, which tries providers in priority order: **Vidsrc → Consumet → ArchiveOrg** (video), **Gutendex → LibriVox** (books/audio), **MangaDex** (manga). Returns `{ streamKind: IframeEmbed | HLS | MP4, url }`. Frontend renders `<ExternalPlayer>` (iframe) or `<VideoPlayer>` (hls.js) accordingly.

### Key Infrastructure Pieces

| Component | Location | Notes |
|---|---|---|
| `MythraDbContext` | `Infrastructure/Persistence/` | SQLite. Auto-touches `UpdatedAt`. Dev startup: `ApplySchemaDeltasAsync()` (idempotent DDL). Production: EF migrations in `Infrastructure/Migrations/`. |
| `MetadataProviderRegistry` | `Infrastructure/Metadata/` | `ConcurrentDictionary`; addons register/unregister at runtime. |
| `ChannelBackgroundJobQueue` | `Infrastructure/Background/` | `System.Threading.Channels`. Enqueue `Func<CancellationToken, Task>`. |
| `LibraryWatcherService` | `Infrastructure/Scanning/` | `FileSystemWatcher` triggers scans on add/remove. |
| `ErrorHandlingMiddleware` | `Api/Middleware/` | Maps exceptions to RFC 7807 `ProblemDetails`. Business errors must flow through `Result<T>`, not exceptions. |
| `CorrelationIdMiddleware` | `Api/Middleware/` | Injects `X-Correlation-ID` for tracing. |
| `SyncPlayHub` | `Api/WebSockets/` | Raw WebSocket at `/ws/sync/{code}`. Commands: `Play`, `Pause`, `Seek`, `ChangeMedia`, `Ready`, `Buffer`. Rooms persisted in SQLite. |

### Authentication

JWT Bearer. `HttpCurrentUser : ICurrentUser` extracts the user ID from `HttpContext.User`. Register / Login / Refresh are unauthenticated; all other endpoints require `[Authorize]`. `JwtOptions`: issuer `mythra`, audience `mythra-clients`, `AccessTokenMinutes=30`, `RefreshTokenDays=30`.

### Addon System

Addons are isolated DLLs loaded via `AssemblyLoadContext` (one ALC per addon, collectible for hot-unload):

1. `AddonHost` discovers DLL folders under `AddonPath`, reads `manifest.json`, instantiates addon class.
2. `AddonSandboxContext` wraps host services (logger, named `HttpClient`, in-memory secret store) and is passed to `IAddon.InitializeAsync()`.
3. For metadata addons: `AddonMetadataBridge` wraps `IMetadataAddon` → `IMetadataProvider` and registers it in `MetadataProviderRegistry`.
4. `IStreamSourceAddon` and `ISubtitleAddon` registries are **not yet implemented** (TODO in `AddonHost`).

To build an addon: reference only `Mythra.Addons.Contracts`, implement `IMetadataAddon`. Drop the DLL folder (DLL + `manifest.json`) under `AddonPath`. See `addons/Mythra.Addon.OmdbMetadata/` for the reference example.

### API Route Summary

| Controller | Base Route | Purpose |
|---|---|---|
| `AuthController` | `/api/v1/auth` | Register, login, refresh, profiles |
| `MediaController` | `/api/v1/items` | List (paginated, filterable), detail, genres, episodes |
| `LibrariesController` | `/api/v1/libraries` | CRUD libraries, folders, extensions, enqueue scan |
| `StreamController` | `/api/v1/stream` | HLS manifest + segments, external stream resolution |
| `ProgressController` | `/api/v1/progress` | Playback + reading progress, bookmarks, highlights |
| `SyncPlayController` + Hub | `/api/v1/syncplay` + `/ws/sync/{code}` | Sync playback rooms |
| `SearchController` | `/api/v1/search` | Full-text search |
| `PlaylistsController` | `/api/v1/playlists` | CRUD playlists |
| `FavoritesController` | `/api/v1/favorites` | Add/remove/list favorites |
| `DiscoverController` | `/api/v1/discover` | Trending, recommendations, external import |
| `AddonsController` | `/api/v1/addons` | List, activate, deactivate, health-check |
| `StatisticsController` | `/api/v1/statistics` | Watch time, top genres |
| `FilesystemController` | `/api/v1/filesystem` | Browse host filesystem (for library setup UI) |
| `DownloadController` | `/api/v1/download/{id}` | Download media files |

---

## Frontend Architecture

**Stack:** Next.js 15 (App Router) · React 19 · TypeScript · Tailwind 4 · Framer Motion · hls.js · Zustand · TanStack Query · Axios · Vitest.

### Page Routes

```
/                  → Home (HeroBanner + ContentRows by library)
/search            → Full-text search
/discover          → Trending, external content browse + import
/library/[id]      → Single library grid
/library/all/[type]→ Cross-library grid by media type
/item/[id]         → Media detail (metadata, cast, episodes, actions)
/watch/[id]        → VideoPlayer (HLS local or ExternalPlayer)
/read/[id]         → BookReader (EPUB/PDF)
/listen/[id]       → AudioPlayer (chapters, speed)
/playlists         → Playlist management
/favorites         → Favorites grid
/statistics        → Personal usage stats
/notifications     → Notification center
/settings          → Libraries, addons, user preferences
/adult             → Adult content gate
/login             → Auth page
```

### State & Data Fetching

**Zustand stores** (persist to `localStorage`):
- `useAuthStore` — `user`, `accessToken`, `refreshToken`, `activeProfile`, `isHydrated`. Exposes `tokenStoreAdapter` for use outside React components.
- `useProfileStore` — active profile switching.
- `useLocaleStore` — language preference.

**Axios `api` instance** (`src/lib/api.ts`): base URL from `NEXT_PUBLIC_API_ORIGIN` (defaults to `/api`). Request interceptor attaches Bearer token. Response interceptor handles 401 → auto-refresh with request deduplication (single in-flight refresh promise). On refresh failure: clears auth store, redirects to `/login`.

**TanStack Query** wraps all `api.*` calls in page/component hooks. Mutations call `queryClient.invalidateQueries` on success.

### Motion Design System (`src/lib/motion.ts`)

All animations must use the tokens here — never ad-hoc values:

```ts
durations: { instant: 0.08, fast: 0.18, medium: 0.32, slow: 0.54, cinematic: 0.9 }
easings:   { outQuint, outExpo, inOut, spring }

Variants: fadeRise, stagger(), cardHover, heroBackdrop, overlayGradient
```

Use GPU-only transforms (`scale`, `translate`, `opacity`, `filter: blur`). Never animate layout properties (`width`, `height`, `padding`, `margin`).

### Brand Tokens (Tailwind / CSS)

Defined in `globals.css` as CSS custom properties:
- Colors: `--color-mythra-purple`, `--color-mythra-blue`, `--color-mythra-magenta`
- Shadows: `--shadow-glow-purple`, `--shadow-glow-blue`
- Use `cn()` (`src/lib/cn.ts`) for conditional class merging (wraps `clsx` + `tailwind-merge`).

### Key Components

| Component | Purpose |
|---|---|
| `<HeroBanner>` | Auto-rotating cinematic hero with `heroBackdrop` animation |
| `<ContentRow>` | Netflix-style horizontal scroll with momentum |
| `<MediaCard>` | Kind-aware card with `cardHover` variant, poster/thumbnail |
| `<VideoPlayer>` | hls.js wrapper, thumbnail seek bar, keyboard shortcuts |
| `<MangaReader>` | RTL/LTR page flip, CBZ rendering |
| `<BookReader>` | EPUB chapters, theme cycling, font sizing |
| `<AudioPlayer>` | Chapter list, playback speed, mini rotation animation |
| `<ExternalPlayer>` | iframe embed for Vidsrc/Consumet external streams |
| `<PageScaffold>` | Layout shell (sidebar + topbar) |
| `<FolderBrowser>` | Calls `FilesystemController` to browse host paths for library setup |

---

## Database Schema Overview

All media lives in a single `MediaItems` table with a TPH discriminator. Key relationships:

```
Users ──< Profiles ──< PlaybackProgress / ReadingProgress / Bookmarks / Highlights
Users ──< Sessions (JWT refresh tokens)
Libraries ──< LibraryFolders
Libraries ──< MediaItems ──< AudioTracks / Subtitles / ChapterMarkers
MediaItems ──< BookChapters / MangaChapters / AudioChapters
Profiles ──< Playlists ──< PlaylistItems
Profiles ──< FavoriteItems
SyncRooms ──< SyncMembers
Addons (manifest + activation status, links to AssemblyLoadContext at runtime)
```

`VideoItem` carries: `VideoKind` (Movie / Episode / Series), `IsAnime`, `ParentId` (series → seasons → episodes), `SeasonNumber`, `EpisodeNumber`, `Duration`, codec/resolution fields.

---

## Testing

| Project | Framework | Tests | Scope |
|---|---|---|---|
| `Mythra.Domain.Tests` | xUnit v3 | 33 | Aggregate invariants, entity logic, value objects |
| `Mythra.Application.Tests` | xUnit v3, Moq, AutoFixture | 6 | Service layer, all interfaces mocked |
| `Mythra.Api.Tests` | xUnit v3, WebApplicationFactory | 2 | HTTP endpoint contracts |
| `frontend` | Vitest | 6 | `cn()`, `motion.ts` tokens |

Domain tests are pure — no DI, no database. Application tests mock every port. Infrastructure tests (external API calls) should be marked `[Trait("Category", "Integration")]`.

---

## Required Configuration

The API degrades silently without these — set them in `appsettings.Development.json` (gitignored) or environment variables:

| Key | Env var | Purpose |
|---|---|---|
| `Jwt:Secret` | `Jwt__Secret` | 32+ chars, required for JWT signing |
| `Metadata:TmdbApiKey` | `Metadata__TmdbApiKey` | TMDb (movies/TV metadata) |
| `Metadata:GoogleBooksApiKey` | `Metadata__GoogleBooksApiKey` | Google Books metadata |

FFmpeg and ffprobe must be on `PATH`. External stream providers (Vidsrc, Consumet, ArchiveOrg, MangaDex, Gutendex, LibriVox) are toggled via the `ExternalProviders` config section and default to enabled where no key is required.

---

## Known Gaps & Next Priorities (from roadmap)

- `IStreamSourceAddon` and `ISubtitleAddon` plugin registries not yet wired in `AddonHost`.
- No addon DLL upload endpoint — addons are dropped manually into `AddonPath` folder.
- No endpoint to list loaded addons from the runtime (`AddonsController` reads DB, not live ALC state).
- EF Core migrations exist in `Infrastructure/Migrations/` but dev uses `ApplySchemaDeltasAsync()` — generate a proper initial migration before any production deploy.
- TMDb image proxy and caching not implemented — posters currently link external CDN URLs directly.
- `BookReader` uses `dangerouslySetInnerHTML` for EPUB body rendering — sanitize with DOMPurify for untrusted libraries.
- Recommendation engine is keyword-relevance only; collaborative filtering deferred.
- Live TV tuner (HDHomeRun style) is intentionally deferred.
