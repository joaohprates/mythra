# Mythra — Roadmap & Status

## ✅ Implemented in 0.1.0 (this session)

### Backend (Clean Architecture, .NET 10)

- Clean architecture solution with `Domain · Application · Infrastructure · Api`
  + `*.Tests` mirrors. Mythra.slnx (.NET 10 XML solution format).
- **Domain** — multi-media entities (`MediaItem` TPH:
  `VideoItem · MangaItem · BookItem · AudioItem`), `Library`, `User · Profile · Session`,
  `PlaybackProgress · ReadingProgress · Bookmark · Highlight`,
  `StreamSession`, `SyncRoom · SyncMember · SyncCommand`,
  `Result<T>`, `DomainException`, domain events.
- **Application** — services for Auth, Libraries, Media, Progress, Streaming,
  SyncPlay, Search, Scanning. DTOs, FluentValidation validators, ports
  (`IMediaProbe · ITranscoder · IMetadataProvider · IBackgroundJobQueue`,
  repositories, `IUnitOfWork`, `IClock`, `IFileSystem`, `ICurrentUser`).
- **Infrastructure** — EF Core 10 + SQLite with full TPH configuration, BCrypt
  hasher, JWT token service, `FfmpegMediaProbe` (real `ffprobe` JSON parser),
  `FfmpegTranscoder` (real HLS spawn + thumbnail extraction),
  `VideoLibraryScanner` (filename heuristics + ffprobe enrichment),
  `MangaLibraryScanner` (CBZ/CBR via SharpCompress),
  `BookLibraryScanner` (EPUB via VersOne.Epub + PDF via PdfPig),
  `AudioLibraryScanner` (TagLibSharp), `MetadataProviderRegistry` with
  `TmdbMetadataProvider · AniListMetadataProvider · GoogleBooksMetadataProvider ·
  MusicBrainzMetadataProvider`, `Channel<T>`-based background queue with
  `BackgroundJobWorker` `IHostedService`.
- **API** — controllers: `AuthController · LibrariesController · MediaController ·
  StreamController · SearchController · ProgressController · SyncPlayController ·
  ContentController · HealthController`. Raw WebSocket `SyncPlayHub` at
  `/ws/sync/{code}`. Serilog request logging + correlation id middleware,
  problem-details error middleware, Swagger v1, Prometheus `/metrics`,
  CORS, JWT bearer auth with role-based authorization.
- 47 passing tests across xUnit v3 (Domain + Application + Api) and Vitest (frontend).

### Frontend (Next.js 15 + React 19)

- Tailwind 4 + Mythra brand tokens (purple / blue / magenta gradients,
  glow shadows, custom motion durations & easings).
- Pages: `/login · / · /search · /library/[id] · /item/[id] · /watch/[id] ·
  /read/[id] · /listen/[id] · /settings`.
- Components: `<Topbar>`, `<HeroBanner>` (auto-rotating cinematic hero),
  `<ContentRow>` (Netflix-style horizontal scroll with momentum), `<MediaCard>`
  (kind-aware cinematic card), `<VideoPlayer>` (hls.js, premium controls,
  thumbnail seek bar gradient, keyboard shortcuts), `<MangaReader>` (RTL/LTR,
  page-flip motion), `<BookReader>` (EPUB chapters, theme cycling, font sizing),
  `<AudioPlayer>` (chapter list, playback speed, mini-rotation animation).
- Zustand auth store with JWT refresh interceptor, TanStack Query for API,
  axios client with token refresh.

### Deployment

- Multi-stage Dockerfiles for API and Web; `docker-compose.yml` with shared volumes.
- `EnsureCreatedAsync` bootstrap for dev so the DB schema is ready on first run.

## 🚧 Known caveats (intentional, scoped out)

- **EF Core migrations** — schema is bootstrapped via `EnsureCreatedAsync` in
  Development. A formal `dotnet ef migrations add Initial` should be generated
  before production deploys.
- **TMDb / Google Books keys** — providers degrade gracefully (return `[]`)
  when keys are absent. Set `Metadata__TmdbApiKey` / `Metadata__GoogleBooksApiKey`
  to unlock real metadata.
- **Live TV** — controllers stubbed via `LibraryKind.Music` / `Anime` falling
  back to existing scanners. Tuner integration (HDHomeRun / Plex DVR style)
  intentionally deferred.
- **Plugin system** — replaced by first-class media kinds. The
  `IMetadataProviderRegistry` and `IMediaScannerRegistry` are the extension
  points; plugin discovery (e.g. via `AssemblyLoadContext`) is future work.
- **Recommendation engine** — `SearchService` does literal-relevance ranking.
  Cross-media recommendation (collaborative filtering, embeddings) deferred.
- **MangaReader / BookReader / AudioPlayer endpoints** — implemented in
  `ContentController`. EPUB body rendering uses `dangerouslySetInnerHTML` from
  the parsed HTML; consider sanitizing through DOMPurify for untrusted libraries.

## 🌌 Next iterations

- Generate EF migration + seed admin user.
- TMDb image proxy + caching (current implementation links external CDNs).
- Subtitle burn-in toggles + per-stream language preference UI.
- Recommendation engine (genre + completion-rate co-occurrence).
- Embedding-based semantic search (sqlite-vss or LiteDB-style).
- Live TV tuner adapter (HDHomeRun) once a sample stream is available.
- Mobile-first layout polish, foldables, TV remote control mode.
- E2E tests with Playwright on top of the 47 existing unit/integration tests.
