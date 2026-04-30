# Mythra

> A personal media universe where every form of storytelling converges.

Mythra is a self-hosted, multi-media streaming platform for movies, TV, anime,
manga, books, and audiobooks — built with cinematic UX as a first-class
concern. Jellyfin-class depth meets Netflix-class polish, with first-class
support for non-video content.

![status](https://img.shields.io/badge/status-0.1.0%20alpha-purple) ![tests](https://img.shields.io/badge/tests-47%20passing-22c55e) ![dotnet](https://img.shields.io/badge/.NET-10.0-blue) ![next](https://img.shields.io/badge/Next.js-15-black)

## Stack

**Backend** — .NET 10 · ASP.NET Core · Clean Architecture · EF Core + SQLite ·
JWT · BCrypt · FFmpeg HLS pipeline · Serilog · Prometheus · xUnit v3 · Moq ·
AutoFixture · FluentAssertions.

**Frontend** — Next.js 15 (App Router) · React 19 · TypeScript · Tailwind 4 ·
Framer Motion · hls.js · Zustand · TanStack Query · Vitest.

**Media types** — Video (movies / TV / anime) · Manga (CBZ / CBR) ·
Books (EPUB / PDF) · Audiobooks. Each gets its own ingestion pipeline,
metadata provider, and reader/player UX — **not plugins, first-class.**

## Repo layout

```
mythra/
├── backend/                    # .NET 10 solution (Mythra.slnx)
│   ├── src/
│   │   ├── Mythra.Domain/          # Entities, value objects, domain events
│   │   ├── Mythra.Application/     # Use cases, services, ports, validators
│   │   ├── Mythra.Infrastructure/  # EF Core, FFmpeg, scanners, providers, jobs
│   │   └── Mythra.Api/             # Controllers, middleware, WS hubs, Swagger
│   └── tests/
│       ├── Mythra.Domain.Tests/        (33 tests)
│       ├── Mythra.Application.Tests/   (6 tests)
│       └── Mythra.Api.Tests/           (2 tests)
├── frontend/                   # Next.js + React 19 + Tailwind 4
│   └── src/{app,components,lib,store,hooks}
├── docker/                     # Dockerfile.api + Dockerfile.web
├── docker-compose.yml          # api + web services with shared volumes
├── docs/
│   ├── ARCHITECTURE.md
│   └── ROADMAP.md
└── media/                      # Local sample / library mounts (gitignored)
```

## Quick start

### Prerequisites

- .NET 10 SDK ≥ 10.0.203
- Node.js ≥ 20 (24 tested)
- FFmpeg + ffprobe on `PATH`
- (Optional) TMDb / Google Books API keys for rich metadata

### Local dev

```bash
# 1. Restore + build backend
cd backend && dotnet build Mythra.slnx

# 2. Install frontend deps
cd frontend && npm install

# 3. Start backend (auto-creates SQLite schema in dev)
cd backend && dotnet run --project src/Mythra.Api
#   API:        http://localhost:5080cla
#   Swagger:    http://localhost:5080/swagger
#   Metrics:    http://localhost:5080/metrics
#   Health:     http://localhost:5080/api/v1/health

# 4. Start frontend
cd ../frontend && npm run dev
#   Web:        http://localhost:3000
```

The first user registered is automatically promoted to `Admin`.

### Docker

```bash
# Set a secret JWT signer (32+ chars)
export MYTHRA_JWT_SECRET="$(openssl rand -base64 48)"
docker compose up -d
```

`./media` on the host is bind-mounted read-only into the API container; add
folders here and create libraries pointing at `/media/...` paths.

## Run all tests

```bash
# Backend
cd backend && dotnet test Mythra.slnx
#   ✔ Mythra.Domain.Tests        33 passed
#   ✔ Mythra.Application.Tests    6 passed
#   ✔ Mythra.Api.Tests            2 passed

# Frontend
cd frontend && npm test
#   ✔ src/lib/__tests__/cn.test.ts        3 passed
#   ✔ src/lib/__tests__/motion.test.ts    3 passed
```

**Total: 47 tests, 0 failing.**

## Brand

Mythra is **cinematic, immersive, intelligent, elegant, futuristic**. The UI
is dark with soft purple/blue/magenta gradients, depth via layered shadows,
and motion as a first-class system (60fps GPU-only transforms, custom
duration & easing tokens, shared element transitions).

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — layers, data flow, design system.
- [docs/ROADMAP.md](docs/ROADMAP.md) — what's done in 0.1.0 and what's next.

## License

Personal, self-hosted use. No external license declared yet.
