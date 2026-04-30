# Mythra — Blueprint Técnico de Arquitetura
### Documento de Planejamento para Desenvolvimento

> **Versão:** 2.0 · **Data:** 2026-04-29  
> Stack: .NET 10 + Next.js 15 + PostgreSQL · Arquitetura: Clean Architecture

---

## Índice

| # | Seção | Descrição |
|---|-------|-----------|
| 1 | [Diagrama de Arquitetura do Sistema de Bibliotecas](#1-diagrama-de-arquitetura-do-sistema-de-bibliotecas) | Componentes, fluxo de dados e integrações |
| 2 | [Fluxo de Importação e Exportação de Mídia](#2-fluxo-de-importação-e-exportação-de-mídia) | Pipelines completos com validação e tratamento de erros |
| 3 | [Estrutura de Banco de Dados](#3-estrutura-de-banco-de-dados) | Schema SQL completo com relacionamentos |
| 4 | [Especificação de APIs e Endpoints](#4-especificação-de-apis-e-endpoints) | Endpoints com exemplos de request/response |
| 5 | [Mapa de Navegação do Site](#5-mapa-de-navegação-do-site) | Todas as páginas, seções e fluxos de usuário |
| 6 | [Estratégia de Implementação Multi-idioma](#6-estratégia-de-implementação-multi-idioma) | UI e metadados de mídia |
| 7 | [Roadmap Técnico Sugerido](#7-roadmap-técnico-sugerido) | Prioridades, dependências e estimativas |

---

## 1. Diagrama de Arquitetura do Sistema de Bibliotecas

### 1.1 Visão Macro — Camadas da Aplicação

```
╔══════════════════════════════════════════════════════════════════════════╗
║                           CLIENTE (Browser)                              ║
║  Next.js 15 App Router · TanStack Query · Zustand · Framer Motion       ║
╠══════════════════════════════════════════════════════════════════════════╣
║                          API GATEWAY (Port 5000)                         ║
║  ASP.NET Core 10 · JWT Bearer · CORS · Rate Limiting · Error Middleware  ║
╠═══════════════════════╦══════════════════════════════════════════════════╣
║   APPLICATION LAYER   ║            INFRASTRUCTURE LAYER                  ║
║  ─────────────────    ║  ──────────────────────────────────────────────  ║
║  LibraryService       ║  EF Core 10 → PostgreSQL (prod) / SQLite (dev)  ║
║  ScanService          ║  LocalFileSystem → /media (volume Docker)        ║
║  ImportService        ║  FFmpeg Probe → extrai metadados de vídeo        ║
║  ExternalProviderSvc  ║  Metadata Providers:                             ║
║  NotificationService  ║    · TMDB (filmes/séries)                        ║
║  RecommendationService║    · AniList (anime/manga) — GraphQL             ║
║  DiscoverService      ║    · Google Books                                ║
║  ─────────────────    ║  External Stream Providers:                      ║
║  DOMAIN LAYER         ║    · Vidsrc (iframe embed, sem API key)          ║
║  Library + Folders    ║    · Consumet (HLS/GogoAnime)                    ║
║  MediaItem (abstract) ║    · Archive.org (MP4 público)                   ║
║  VideoItem            ║    · Gutenberg / LibriVox / MangaDex             ║
║  BookItem             ║  Background Jobs (Channel<T>):                   ║
║  MangaItem            ║    · ScanLibraryJob                              ║
║  AudioItem            ║    · ImportFromUrlJob                            ║
║  Notification         ║    · MetadataRefreshJob                          ║
║  Profile / User       ║    · ProviderHealthCheckJob                      ║
╚═══════════════════════╩══════════════════════════════════════════════════╝
```

---

### 1.2 Diagrama do Sistema de Bibliotecas

```
 ┌─────────────────────────────────────────────────────────────────────┐
 │                    SISTEMA DE BIBLIOTECAS MYTHRA                    │
 └─────────────────────────────────────────────────────────────────────┘

  BOOT DA APLICAÇÃO
  ══════════════════
  ┌──────────────────────┐
  │  LibraryBootstrapSvc │──► Biblioteca "General" existe?
  └──────────────────────┘         │
           NÃO ◄──────────────────┘└──────────────── SIM
            │                                          │
            ▼                                          ▼
   Criar Library {                           Prosseguir normalmente
     Name = "General"
     Kind = General        ◄──── IsSystem = true (não pode ser deletada)
     IsSystem = true
     Folder = "/media"
   }

  HIERARQUIA DE BIBLIOTECAS
  ══════════════════════════

  ┌────────────────────────────────────────────────────────────────────┐
  │  Library: "General"  (Kind=General · IsSystem=true · /media)       │
  │  ┌──────────────────────────────────────────────────────────┐      │
  │  │  DETECTOR AUTOMÁTICO DE TIPO                             │      │
  │  │  .mp4 .mkv .avi .webm .ts  ────────────►  VideoItem      │      │
  │  │  .epub .pdf .mobi .azw3    ────────────►  BookItem       │      │
  │  │  .cbz .cbr .cb7            ────────────►  MangaItem      │      │
  │  │  .mp3 .flac .m4a .ogg      ────────────►  AudioItem      │      │
  │  │  outros                    ────────────►  GenericItem    │      │
  │  └──────────────────────────────────────────────────────────┘      │
  └────────────────────────────────────────────────────────────────────┘
           │
           │  Usuário pode criar sub-bibliotecas tipadas:
           │
  ┌────────┴────────────────────────────────────────────────────────┐
  │  Library: "Filmes"     Kind=Video   Folder=/media/filmes        │
  │  Library: "Manga"      Kind=Manga   Folder=/media/manga         │
  │  Library: "Livros"     Kind=Book    Folder=/media/livros        │
  │  Library: "Música"     Kind=Music   Folder=/media/musica        │
  │  Library: "External"   Kind=Video   (sem FilePath — streaming)  │
  └────────────────────────────────────────────────────────────────-┘


  FLUXO DE DADOS — DO ARQUIVO AO ITEM NA UI
  ══════════════════════════════════════════

  /media/filmes/inception.mkv
          │
          ▼
  ┌───────────────────┐
  │   FILE WATCHER    │  (InotifyWaitAsync / PollingWatcher)
  │   ou Scan Manual  │
  └────────┬──────────┘
           │ path detectado
           ▼
  ┌───────────────────┐    duplicado?  ┌──────────────────┐
  │  DuplicateChecker │──────── SIM ──►│  Skip silencioso │
  │  (hash SHA-256)   │                └──────────────────┘
  └────────┬──────────┘
           │ novo arquivo
           ▼
  ┌───────────────────┐
  │   MediaProbe      │  FFmpeg → extrai: codec, resolução,
  │   (FFmpeg)        │  duração, faixas de áudio e legendas
  └────────┬──────────┘
           │
           ▼
  ┌───────────────────┐
  │  MetadataFetcher  │  detecta IMDB/TMDB via nome do arquivo
  │  TMDB / AniList   │  → busca poster, sinopse, gêneros
  └────────┬──────────┘
           │
           ▼
  ┌───────────────────┐
  │  MediaItem criado │  salvo no PostgreSQL
  │  no banco de dados│  evento MediaItemAdded disparado
  └────────┬──────────┘
           │
           ▼
  ┌───────────────────┐    push SSE
  │  NotificationSvc  │───────────────► Browser
  │  "Item adicionado"│                 Bell icon badge +1
  └───────────────────┘
```

---

### 1.3 Componentes e suas Responsabilidades

| Componente | Camada | Responsabilidade |
|---|---|---|
| `Library` | Domain | Agregado raiz — nome, kind, pastas, configurações |
| `LibraryFolder` | Domain | Path físico, status ativo/inativo, data do último scan |
| `LibraryBootstrapService` | Application | Garante biblioteca General no boot |
| `LibraryService` | Application | CRUD de bibliotecas, validação de regras de negócio |
| `ScanService` | Application | Orquestra scan, chama IMediaScannerRegistry |
| `IMediaScannerRegistry` | Application | Resolve o scanner certo para cada LibraryKind |
| `LocalFileSystem` | Infrastructure | Abstração sobre System.IO — lista, lê, verifica arquivos |
| `IMediaProbe` | Infrastructure | FFmpeg — extrai metadados técnicos de vídeo/áudio |
| `BackgroundJobWorker` | Infrastructure | Executa ScanLibraryJob em background via Channel<T> |

---

## 2. Fluxo de Importação e Exportação de Mídia

### 2.1 Pipeline de Importação — Arquivo Local

```
FONTE: Arquivo copiado para /media ou pasta de biblioteca

  ┌─────────────────────────────────────────────────────────────┐
  │                   PIPELINE DE IMPORTAÇÃO LOCAL               │
  └─────────────────────────────────────────────────────────────┘

  PASSO 1 — DESCOBERTA
  ─────────────────────
  POST /api/v1/libraries/{id}/scan
        │
        ▼
  ScanService.RunAsync()
  ├── Valida: biblioteca existe?              → 404 se não
  ├── Valida: scanner registrado para o kind?  → 400 se não
  ├── Valida: tem pastas ativas?               → 400 se não
  └── Enfileira ScanLibraryJob via Channel<T>

  PASSO 2 — VARREDURA DO SISTEMA DE ARQUIVOS
  ───────────────────────────────────────────
  ScanLibraryJob.ExecuteAsync()
        │
        ├── Para cada pasta da biblioteca:
        │     LocalFileSystem.EnumerateFiles(path, extensions)
        │       └── Filtra por GetEffectiveExtensions()
        │
        └── Para cada arquivo encontrado:
              │
              ▼
  ┌─────────────────────────────────────────────┐
  │           VALIDAÇÃO DO ARQUIVO              │
  │                                             │
  │  ① Tamanho > 0 bytes?                       │
  │        NÃO → log warning, skip              │
  │                                             │
  │  ② Extensão na allowlist do library?        │
  │        NÃO → skip silencioso                │
  │                                             │
  │  ③ Já indexado? (busca por FilePath)        │
  │        SIM → verificar se modificado        │
  │              (LastWriteTime diferente?)     │
  │              SIM → UPDATE do item           │
  │              NÃO → skip                     │
  │                                             │
  │  ④ Hash SHA-256 calculado → duplicata?      │
  │        SIM → link para item existente       │
  │        NÃO → criar novo MediaItem           │
  └──────────────────┬──────────────────────────┘
                     │ arquivo válido e novo
                     ▼

  PASSO 3 — EXTRAÇÃO DE METADADOS TÉCNICOS
  ─────────────────────────────────────────
  IMediaProbe.ProbeAsync(filePath)
  └── FFmpeg -v quiet -print_format json -show_streams -show_format
      Extrai:
        · container (mkv, mp4, epub...)
        · codec de vídeo e áudio
        · resolução (width × height)
        · duração (TimeSpan)
        · faixas de áudio (idioma, codec, canais)
        · legendas embarcadas (idioma, formato)
        · bitrate
        · capítulos

  PASSO 4 — ENRIQUECIMENTO DE METADADOS
  ──────────────────────────────────────
  IMetadataProvider.FetchAsync(title, year, kind)
  ├── Tenta TMDB primeiro (para vídeos)
  │     · Match por nome do arquivo normalizado
  │     · Busca poster (1080×1580px) e backdrop
  │     · Busca géneros, sinopse, rating
  │     · Salva ProviderTmdbId
  │
  ├── Fallback AniList (se IsAnime=true)
  └── Fallback: apenas dados técnicos (sem poster)

  PASSO 5 — PERSISTÊNCIA
  ──────────────────────
  MediaItemRepository.AddAsync(item)
  UnitOfWork.SaveChangesAsync()
  └── Dispara evento de domínio: MediaItemAdded

  PASSO 6 — NOTIFICAÇÃO
  ─────────────────────
  DomainEventHandler(MediaItemAdded)
  └── NotificationService.CreateAsync({
        Kind: MediaAdded,
        Title: "{título} foi adicionado",
        ActionUrl: "/item/{id}",
        ImageUrl: posterPath
      })
      └── SSE push → todos os clientes conectados

  RESULTADO: ScanResult { Added, Updated, Removed, Failed, Elapsed }
```

---

### 2.2 Pipeline de Importação — Fonte Externa (Discover)

```
FONTE: Usuário encontra item via /discover e clica "Add to Library"

  POST /api/v1/discover/import
  Body: {
    "providerKind": "Tmdb",
    "externalId": "155",
    "mediaKind": "Video",
    "targetLibraryId": null  ← null = auto-criar "External Videos"
  }

        │
        ▼
  ┌─────────────────────────────────────────────────────────────┐
  │              PIPELINE DE IMPORTAÇÃO EXTERNA                  │
  └─────────────────────────────────────────────────────────────┘

  PASSO 1 — VALIDAÇÃO
  ──────────────────
  ① mediaKind é suportado? (Video/Book/Manga/Audio)   → 400 se não
  ② externalId não está vazio?                         → 400 se não
  ③ Item já importado? (busca por ProviderTmdbId etc.) → 409 Conflict
  ④ providerKind é conhecido?                          → 400 se não

  PASSO 2 — RESOLUÇÃO DE BIBLIOTECA ALVO
  ──────────────────────────────────────
  Se targetLibraryId = null:
    Busca Library com Name = "External {kind}" (ex: "External Videos")
    NÃO existe → criar automaticamente:
      Library {
        Name: "External Videos",
        Kind: Video,
        IsSystem: false,
        Description: "Conteúdo importado via Discover"
      }

  PASSO 3 — BUSCA DE METADADOS
  ─────────────────────────────
  IMetadataProviderRegistry.Resolve(providerKind)
    .FetchByIdAsync(externalId)
  └── Retorna: título, sinopse, poster, genres, rating, year
      Salva imagens em /data/images/{itemId}/

  PASSO 4 — CRIAÇÃO DO ITEM (SEM ARQUIVO)
  ─────────────────────────────────────────
  VideoItem {
    LibraryId:      resolvedLibraryId,
    Title:          "The Dark Knight",
    FilePath:       null,           ← CHAVE: sem arquivo local
    HasFile:        false,
    ProviderImdbId: "tt0468569",
    ProviderTmdbId: "155",
    PosterPath:     "/data/images/{id}/poster.jpg",
    BackdropPath:   "/data/images/{id}/backdrop.jpg",
    FileStatus:     ExternalOnly
  }

  PASSO 5 — STREAMING ON-DEMAND
  ──────────────────────────────
  Quando usuário abre /watch/{id}:

    GET /api/v1/stream/external/{id}
          │
          ▼
    ExternalProviderService.GetVideoStreamAsync()
    ├── Priority 10: VidsrcProvider
    │     └── Constrói URL: vidsrc.xyz/embed/movie/tt0468569
    │         Retorna { StreamKind: IframeEmbed, Url: "..." }
    │
    ├── Priority 20: ConsumetProvider (fallback — HLS)
    └── Priority 30: ArchiveOrgProvider (fallback — MP4)

    Frontend renderiza <ExternalPlayer> com <iframe> ou <video>
```

---

### 2.3 Pipeline de Exportação

```
TIPOS DE EXPORTAÇÃO SUPORTADOS

  ┌────────────────────────────────────────────────────────────────┐
  │  TIPO 1: Download do Arquivo Original                          │
  │                                                                │
  │  GET /api/v1/items/{id}/download                              │
  │       │                                                        │
  │       ├── Valida: item existe? HasFile = true?                 │
  │       │     NÃO → 404 / 422 "Item sem arquivo local"          │
  │       ├── Valida: usuário tem permissão?                       │
  │       ├── Resolve FilePath no LocalFileSystem                  │
  │       ├── Abre FileStream                                      │
  │       └── Retorna FileStreamResult com headers:               │
  │             Content-Type: video/x-matroska                     │
  │             Content-Disposition: attachment; filename="..."    │
  │             Content-Length: {fileSize}                         │
  └────────────────────────────────────────────────────────────────┘

  ┌────────────────────────────────────────────────────────────────┐
  │  TIPO 2: Export de Metadados                                   │
  │                                                                │
  │  GET /api/v1/items/{id}/export?format=nfo                     │
  │                                                                │
  │  Formatos suportados:                                          │
  │  · nfo  → XML Kodi-compatible (importável em outros servidores)│
  │  · json → JSON com todos os campos do MediaItem               │
  │  · csv  → linha única (para exportação de biblioteca)          │
  │                                                                │
  │  Exemplo NFO gerado:                                           │
  │  <movie>                                                       │
  │    <title>The Dark Knight</title>                              │
  │    <year>2008</year>                                           │
  │    <rating>9.0</rating>                                        │
  │    <plot>When the menace known as...</plot>                    │
  │    <uniqueid type="imdb">tt0468569</uniqueid>                  │
  │    <uniqueid type="tmdb">155</uniqueid>                        │
  │    <genre>Action</genre>                                       │
  │    <genre>Crime</genre>                                        │
  │  </movie>                                                      │
  └────────────────────────────────────────────────────────────────┘

  ┌────────────────────────────────────────────────────────────────┐
  │  TIPO 3: Export de Catálogo Completo                           │
  │                                                                │
  │  GET /api/v1/libraries/{id}/export?format=csv                 │
  │                                                                │
  │  Gera arquivo com todos os MediaItems da biblioteca:           │
  │  id, title, kind, year, rating, genres, filePath, hasFile,    │
  │  providerTmdbId, providerImdbId, createdAt                     │
  │                                                                │
  │  Útil para: migração, backup de catálogo, análise             │
  └────────────────────────────────────────────────────────────────┘

  ┌────────────────────────────────────────────────────────────────┐
  │  TIPO 4: Streaming Externo (já implementado)                   │
  │                                                                │
  │  GET /api/v1/stream/external/{id}?season=1&episode=1          │
  │  → ExternalVideoStreamDto { providerName, streamKind, url }   │
  │                                                                │
  │  GET /api/v1/stream/external/{id}/links                       │
  │  → List<ExternalBookLinkDto> { format, url, providerName }    │
  └────────────────────────────────────────────────────────────────┘


  PONTOS DE VALIDAÇÃO E TRATAMENTO DE ERROS
  ══════════════════════════════════════════

  ┌──────────────────────────────────────────────────────────────────┐
  │  Erro                       │ HTTP │ Ação                        │
  ├──────────────────────────────────────────────────────────────────┤
  │  Item não existe            │ 404  │ { "error": "NotFound" }     │
  │  Arquivo não encontrado     │ 422  │ Marcar FileStatus=FileNotFound│
  │  Permissão negada           │ 403  │ { "error": "Forbidden" }    │
  │  Formato não suportado      │ 400  │ { "error": "Validation" }   │
  │  Item sem arquivo (download)│ 422  │ "Use stream externo"        │
  │  Provider indisponível      │ 503  │ Tentar próximo na lista     │
  │  Todos providers falharam   │ 503  │ { "error": "NoStream" }     │
  │  Duplicata na importação    │ 409  │ { "existingId": "..." }     │
  └──────────────────────────────────────────────────────────────────┘
```

---

## 3. Estrutura de Banco de Dados

### 3.1 Diagrama de Relacionamentos (ERD)

```
 ┌──────────┐       ┌──────────────┐      ┌──────────────────┐
 │  Users   │1─────<│   Sessions   │      │    Libraries     │
 │──────────│       │  (JWT/Auth)  │      │──────────────────│
 │ Id       │       └──────────────┘      │ Id               │
 │ Email    │                             │ Name             │
 │ Username │1─────<┌──────────────┐      │ Kind             │
 │ Role     │       │   Profiles   │      │ IsSystem         │
 │ Language │       │──────────────│      │ IsEnabled        │
 └──────────┘       │ Id           │      │ AllowedExtensions│
                    │ UserId       │      │ PreferredLanguage│
                    │ Name         │      │ LastScannedAt    │
                    │ IsKidFriendly│      └──────┬───────────┘
                    │ ContentLang  │             │1
                    │ SubtitleLang │             │
                    │ AudioLang    │      ┌──────┴───────────┐
                    │ ShowOrgTitle │      │  LibraryFolders  │
                    └──────┬───────┘      │──────────────────│
                           │              │ Id               │
                    ┌──────┼──────┐       │ LibraryId        │
                    │      │      │       │ Path             │
                    ▼      ▼      ▼       │ IsActive         │
             Progress  Notifs  WatchLists │ LastScannedAt    │
                                         └──────────────────┘

 ┌──────────────────────────────────────────────────────────────┐
 │                      MediaItems (abstract)                    │
 │──────────────────────────────────────────────────────────────│
 │ Id · LibraryId · Kind · Title · OriginalTitle · SortTitle    │
 │ Overview · Tagline · PosterPath · BackdropPath · ThumbPath   │
 │ ReleaseDate · Rating · RatingCount · Language · CountryCode  │
 │ ProviderTmdbId · ProviderImdbId · ProviderAnilistId          │
 │ ProviderGutenbergId · ProviderLibriVoxId · ProviderMangaDexId│
 │ LocalizedTitles(JSONB) · LocalizedOverviews(JSONB)           │
 │ FileStatus · LastScannedAt · LastMetadataRefreshAt           │
 │ CreatedAt · UpdatedAt                                        │
 └──────────────────────────────────────────────────────────────┘
          │                │                │               │
          ▼                ▼                ▼               ▼
   ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐
   │ VideoItems │  │  BookItems │  │ MangaItems │  │ AudioItems │
   │────────────│  │────────────│  │────────────│  │────────────│
   │ FilePath   │  │ FilePath   │  │ FilePath   │  │ FilePath   │
   │ FileSizeB  │  │ Author     │  │ Author     │  │ Author     │
   │ VideoKind  │  │ Publisher  │  │ Artist     │  │ Narrator   │
   │ IsAnime    │  │ ISBN       │  │ Status     │  │ AudioKind  │
   │ Duration   │  │ Series     │  │ Direction  │  │ Duration   │
   │ Container  │  │ SeriesIdx  │  │ TotalCh    │  │ CoverPath  │
   │ VideoCodec │  │ Format     │  │ TotalVol   │  │ Series     │
   │ AudioCodec │  │ PageCount  │  │            │  │ SeriesIdx  │
   │ Width/Height│  │ WordCount  │  │            │  │            │
   │ FrameRate  │  │            │  │            │  │            │
   │ Bitrate    │  │            │  │            │  │            │
   │ ParentId   │  │            │  │            │  │            │
   │ SeasonNum  │  │            │  │            │  │            │
   │ EpisodeNum │  │            │  │            │  │            │
   └──────┬─────┘  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘
          │              │               │               │
    ┌─────┴──────┐  ┌────┴────┐    ┌────┴────┐    ┌────┴────┐
    │AudioTracks │  │BookChaps│    │MangaChap│    │AudioChap│
    │Subtitles   │  └─────────┘    └─────────┘    └─────────┘
    │ChapterMrkrs│
    └────────────┘

 ┌──────────────────┐     ┌────────────────────┐
 │ PlaybackProgress │     │  ReadingProgress    │
 │──────────────────│     │────────────────────│
 │ ProfileId        │     │ ProfileId           │
 │ MediaItemId      │     │ MediaItemId         │
 │ Position         │     │ CurrentChapterId    │
 │ Duration         │     │ CurrentPage         │
 │ IsCompleted      │     │ CfiLocator          │
 │ PercentComplete  │     │ PercentComplete     │
 │ PlaybackSpeed    │     │ IsCompleted         │
 │ LastWatchedAt    │     │ LastReadAt          │
 └──────────────────┘     └────────────────────┘

 ┌────────────────────────┐     ┌─────────────────────────┐
 │     Notifications      │     │    ProviderHealthChecks  │
 │────────────────────────│     │─────────────────────────│
 │ Id · UserId · ProfileId│     │ ProviderName             │
 │ Kind · Title · Body    │     │ IsHealthy                │
 │ ActionUrl · ImageUrl   │     │ LatencyMs                │
 │ IsRead · Payload(JSONB)│     │ ErrorMessage             │
 │ CreatedAt              │     │ LastCheckedAt            │
 └────────────────────────┘     └─────────────────────────┘
```

---

### 3.2 Schema SQL Completo

```sql
-- ══════════════════════════════════════════════════════════════
-- USUÁRIOS E PERFIS
-- ══════════════════════════════════════════════════════════════

CREATE TABLE "Users" (
    "Id"                UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email"             TEXT        NOT NULL UNIQUE,
    "Username"          TEXT        NOT NULL UNIQUE,
    "PasswordHash"      TEXT        NOT NULL,
    "Role"              TEXT        NOT NULL DEFAULT 'User',
    "AvatarPath"        TEXT,
    "PreferredLanguage" TEXT        NOT NULL DEFAULT 'en',   -- idioma da UI
    "IsActive"          BOOLEAN     NOT NULL DEFAULT TRUE,
    "CreatedAt"         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Sessions" (
    "Id"           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"       UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "RefreshToken" TEXT        NOT NULL UNIQUE,
    "ExpiresAt"    TIMESTAMPTZ NOT NULL,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UserAgent"    TEXT,
    "IpAddress"    TEXT
);

CREATE TABLE "Profiles" (
    "Id"                        UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"                    UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Name"                      TEXT        NOT NULL,
    "AvatarPath"                TEXT,
    "IsKidFriendly"             BOOLEAN     NOT NULL DEFAULT FALSE,
    "Theme"                     TEXT        NOT NULL DEFAULT 'mythra-dark',
    "EnabledMediaKinds"         TEXT[]      NOT NULL DEFAULT '{Video,Manga,Book,Audio}',
    -- Preferências de idioma de conteúdo (NOVO)
    "PreferredContentLanguage"  TEXT,                   -- ex: "pt-BR", "en", "ja"
    "PreferredSubtitleLanguage" TEXT,                   -- ex: "pt", "en"
    "PreferredAudioLanguage"    TEXT,                   -- ex: "pt", "ja"
    "ShowOriginalTitle"         BOOLEAN     NOT NULL DEFAULT FALSE,
    "CreatedAt"                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ══════════════════════════════════════════════════════════════
-- BIBLIOTECAS
-- ══════════════════════════════════════════════════════════════

CREATE TABLE "Libraries" (
    "Id"                       UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name"                     TEXT        NOT NULL UNIQUE,
    "Kind"                     INTEGER     NOT NULL,
    -- Kind: 1=Video 2=Anime 3=Manga 4=Book 5=Audiobook 6=Music 7=General 8=Image
    "Description"              TEXT,
    "IsSystem"                 BOOLEAN     NOT NULL DEFAULT FALSE,  -- NOVO
    "IsEnabled"                BOOLEAN     NOT NULL DEFAULT TRUE,
    "AutoRefreshMetadata"      BOOLEAN     NOT NULL DEFAULT TRUE,
    "PreferredLanguage"        TEXT,
    "PreferredMetadataProvider" TEXT,
    "AllowedExtensions"        TEXT[]      NOT NULL DEFAULT '{}',   -- NOVO ([] = usar padrão do kind)
    "LastScannedAt"            TIMESTAMPTZ,
    "CreatedAt"                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"                TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "LibraryFolders" (
    "Id"            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "LibraryId"     UUID        NOT NULL REFERENCES "Libraries"("Id") ON DELETE CASCADE,
    "Path"          TEXT        NOT NULL,
    "IsActive"      BOOLEAN     NOT NULL DEFAULT TRUE,
    "LastScannedAt" TIMESTAMPTZ,
    UNIQUE ("LibraryId", "Path")
);

-- ══════════════════════════════════════════════════════════════
-- MÍDIA (herança por tabela única — TPH via discriminador)
-- ══════════════════════════════════════════════════════════════

CREATE TABLE "MediaItems" (
    "Id"                       UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "LibraryId"                UUID        NOT NULL REFERENCES "Libraries"("Id") ON DELETE CASCADE,
    "Kind"                     TEXT        NOT NULL,   -- discriminador EF Core TPH
    "Title"                    TEXT        NOT NULL,
    "OriginalTitle"             TEXT,
    "SortTitle"                TEXT,
    "Overview"                 TEXT,
    "Tagline"                  TEXT,
    "PosterPath"               TEXT,
    "BackdropPath"             TEXT,
    "ThumbPath"                TEXT,
    "ReleaseDate"              DATE,
    "Rating"                   DECIMAL(4,2),
    "RatingCount"              INTEGER,
    "Language"                 TEXT,
    "CountryCode"              TEXT,
    -- Metadados localizados (NOVO)
    "LocalizedTitles"          JSONB,   -- {"pt-BR": "O Cavaleiro das Trevas", "es": "El Caballero Oscuro"}
    "LocalizedOverviews"       JSONB,   -- {"pt-BR": "Sinopse em português..."}
    -- Provider IDs
    "ProviderTmdbId"           TEXT,
    "ProviderImdbId"           TEXT,
    "ProviderAnilistId"        TEXT,
    "ProviderMalId"            TEXT,
    "ProviderMusicbrainzId"    TEXT,
    "ProviderGoogleBooksId"    TEXT,
    "ProviderGutenbergId"      TEXT,    -- NOVO
    "ProviderLibriVoxId"       TEXT,    -- NOVO
    "ProviderMangaDexId"       TEXT,    -- NOVO
    "ProviderArchiveOrgId"     TEXT,    -- NOVO
    -- Status do arquivo (NOVO)
    "FileStatus"               TEXT     NOT NULL DEFAULT 'Available',
    -- FileStatus: Available | FileNotFound | ExternalOnly | Downloading
    -- Timestamps
    "LastScannedAt"            TIMESTAMPTZ,
    "LastMetadataRefreshAt"    TIMESTAMPTZ,
    "CreatedAt"                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"                TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- ── Campos exclusivos de VideoItem ──────────────────────
    "VideoKind"         TEXT,     -- Movie|Series|Season|Episode|Anime|...
    "IsAnime"           BOOLEAN,
    "FilePath"          TEXT,
    "FileSizeBytes"     BIGINT,
    "Container"         TEXT,
    "VideoCodec"        TEXT,
    "AudioCodec"        TEXT,
    "Width"             INTEGER,
    "Height"            INTEGER,
    "FrameRate"         DECIMAL(6,3),
    "Bitrate"           BIGINT,
    "Duration_Ticks"    BIGINT,   -- armazenado como ticks .NET → TimeSpan
    "ParentId"          UUID      REFERENCES "MediaItems"("Id") ON DELETE SET NULL,
    "SeasonNumber"      INTEGER,
    "EpisodeNumber"     INTEGER,
    "AbsoluteEpisodeNum" INTEGER,

    -- ── Campos exclusivos de BookItem ───────────────────────
    "Author"            TEXT,
    "Publisher"         TEXT,
    "ISBN"              TEXT,
    "Series"            TEXT,
    "SeriesIndex"       DECIMAL(6,2),
    "BookFormat"        TEXT,     -- Epub|Pdf|Mobi|Azw3|Cbz
    "PageCount"         INTEGER,
    "WordCount"         INTEGER,
    "BookFilePath"      TEXT,

    -- ── Campos exclusivos de MangaItem ──────────────────────
    "MangaAuthor"       TEXT,
    "MangaArtist"       TEXT,
    "Status"            TEXT,     -- Ongoing|Completed|Hiatus|Cancelled
    "ReadingDirection"  TEXT,     -- LeftToRight|RightToLeft|Vertical
    "TotalChapters"     INTEGER,
    "TotalVolumes"      INTEGER,
    "MangaFilePath"     TEXT,

    -- ── Campos exclusivos de AudioItem ──────────────────────
    "AudioAuthor"       TEXT,
    "Narrator"          TEXT,
    "AudioSeries"       TEXT,
    "AudioSeriesIndex"  DECIMAL(6,2),
    "AudioKind"         TEXT,     -- Audiobook|Podcast|Music|Soundtrack
    "AudioDuration_T"   BIGINT,
    "CoverPath"         TEXT,
    "AudioFilePath"     TEXT
);

-- Índices críticos para performance
CREATE INDEX idx_media_library    ON "MediaItems"("LibraryId");
CREATE INDEX idx_media_kind       ON "MediaItems"("Kind");
CREATE INDEX idx_media_created    ON "MediaItems"("CreatedAt" DESC);
CREATE INDEX idx_media_tmdb       ON "MediaItems"("ProviderTmdbId") WHERE "ProviderTmdbId" IS NOT NULL;
CREATE INDEX idx_media_title_fts  ON "MediaItems" USING gin(to_tsvector('simple', "Title"));
CREATE INDEX idx_media_localized  ON "MediaItems" USING gin("LocalizedTitles");
CREATE INDEX idx_media_file_status ON "MediaItems"("FileStatus");

-- ── Sub-tabelas de VideoItem ─────────────────────────────────
CREATE TABLE "Subtitles" (
    "Id"           UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    "VideoItemId"  UUID    NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "LanguageCode" TEXT    NOT NULL,   -- ISO 639-1: "pt", "en", "ja"
    "DisplayName"  TEXT,
    "Format"       TEXT    NOT NULL,   -- srt|ass|vtt|pgs|dvd
    "Kind"         TEXT    NOT NULL,   -- Normal|SDH|Forced
    "IsDefault"    BOOLEAN NOT NULL DEFAULT FALSE,
    "IsForced"     BOOLEAN NOT NULL DEFAULT FALSE,
    "FilePath"     TEXT                -- externo ao arquivo de vídeo
);

CREATE TABLE "AudioTracks" (
    "Id"            UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    "VideoItemId"   UUID    NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "LanguageCode"  TEXT    NOT NULL,
    "DisplayName"   TEXT,
    "StreamIndex"   INTEGER NOT NULL,
    "Codec"         TEXT    NOT NULL,
    "Channels"      INTEGER NOT NULL,
    "ChannelLayout" TEXT    NOT NULL,
    "IsDefault"     BOOLEAN NOT NULL DEFAULT FALSE,
    "IsCommentary"  BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE "ChapterMarkers" (
    "Id"          UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    "MediaItemId" UUID    NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "Kind"        TEXT    NOT NULL,   -- Chapter|Intro|Outro|Recap
    "Label"       TEXT,
    "Start_Ticks" BIGINT  NOT NULL,
    "End_Ticks"   BIGINT,
    "ThumbPath"   TEXT
);

-- ── Sub-tabelas compartilhadas ───────────────────────────────
CREATE TABLE "Genres" (
    "Id"          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MediaItemId" UUID NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "Name"        TEXT NOT NULL
);
CREATE INDEX idx_genres_item ON "Genres"("MediaItemId");
CREATE INDEX idx_genres_name ON "Genres"("Name");

CREATE TABLE "Tags" (
    "Id"          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MediaItemId" UUID NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "Name"        TEXT NOT NULL
);

CREATE TABLE "MediaPersonRoles" (
    "Id"          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MediaItemId" UUID NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "PersonName"  TEXT NOT NULL,
    "Role"        TEXT NOT NULL,   -- Director|Actor|Writer|Producer|etc.
    "Character"   TEXT,
    "Order"       INTEGER,
    "PhotoPath"   TEXT
);

-- ── Capítulos ────────────────────────────────────────────────
CREATE TABLE "BookChapters" (
    "Id"         UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    "BookItemId" UUID    NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "Order"      INTEGER NOT NULL,
    "Title"      TEXT    NOT NULL,
    "Anchor"     TEXT,
    "StartPage"  INTEGER,
    "EndPage"    INTEGER
);

CREATE TABLE "MangaChapters" (
    "Id"           UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    "MangaItemId"  UUID           NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "VolumeNumber" INTEGER,
    "ChapterNumber" DECIMAL(6,2)  NOT NULL,
    "Title"        TEXT,
    "PageCount"    INTEGER        NOT NULL DEFAULT 0,
    "CoverPath"    TEXT,
    "ReleaseDate"  DATE,
    "FilePath"     TEXT
);

CREATE TABLE "AudioChapters" (
    "Id"          UUID   PRIMARY KEY DEFAULT gen_random_uuid(),
    "AudioItemId" UUID   NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "Order"       INTEGER NOT NULL,
    "Title"       TEXT    NOT NULL,
    "Start_Ticks" BIGINT  NOT NULL,
    "Duration_T"  BIGINT  NOT NULL
);

-- ══════════════════════════════════════════════════════════════
-- PROGRESSO
-- ══════════════════════════════════════════════════════════════

CREATE TABLE "PlaybackProgress" (
    "ProfileId"       UUID           NOT NULL REFERENCES "Profiles"("Id") ON DELETE CASCADE,
    "MediaItemId"     UUID           NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "Position_Ticks"  BIGINT         NOT NULL DEFAULT 0,
    "Duration_Ticks"  BIGINT,
    "IsCompleted"     BOOLEAN        NOT NULL DEFAULT FALSE,
    "PercentComplete" DECIMAL(5,2)   NOT NULL DEFAULT 0,
    "PlaybackSpeed"   DECIMAL(3,1)   NOT NULL DEFAULT 1.0,
    "LastWatchedAt"   TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    PRIMARY KEY ("ProfileId", "MediaItemId")
);

CREATE TABLE "ReadingProgress" (
    "ProfileId"        UUID         NOT NULL REFERENCES "Profiles"("Id") ON DELETE CASCADE,
    "MediaItemId"      UUID         NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "CurrentChapterId" UUID         REFERENCES "BookChapters"("Id") ON DELETE SET NULL,
    "CurrentPage"      INTEGER,
    "TotalPages"       INTEGER,
    "CfiLocator"       TEXT,        -- EPUB CFI string
    "PercentComplete"  DECIMAL(5,2) NOT NULL DEFAULT 0,
    "IsCompleted"      BOOLEAN      NOT NULL DEFAULT FALSE,
    "LastReadAt"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    PRIMARY KEY ("ProfileId", "MediaItemId")
);

-- ══════════════════════════════════════════════════════════════
-- NOTIFICAÇÕES E RECOMENDAÇÕES
-- ══════════════════════════════════════════════════════════════

CREATE TABLE "Notifications" (
    "Id"        UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"    UUID        REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ProfileId" UUID        REFERENCES "Profiles"("Id") ON DELETE SET NULL,
    -- UserId=null e ProfileId=null → broadcast global
    "Kind"      INTEGER     NOT NULL,
    -- 1=MediaAdded 2=ScanCompleted 3=ScanFailed 4=Recommendation
    -- 5=ImportCompleted 6=ProviderUnhealthy 99=System
    "Title"     TEXT        NOT NULL,
    "Body"      TEXT,
    "ActionUrl" TEXT,       -- ex: "/item/uuid-aqui"
    "ImageUrl"  TEXT,       -- URL do poster
    "IsRead"    BOOLEAN     NOT NULL DEFAULT FALSE,
    "Payload"   JSONB,      -- dados extras: {"mediaItemId": "...", "libraryId": "..."}
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user_unread
    ON "Notifications"("UserId", "IsRead", "CreatedAt" DESC)
    WHERE "IsRead" = FALSE;
CREATE INDEX idx_notifications_profile
    ON "Notifications"("ProfileId", "CreatedAt" DESC)
    WHERE "ProfileId" IS NOT NULL;

-- ══════════════════════════════════════════════════════════════
-- INFRAESTRUTURA
-- ══════════════════════════════════════════════════════════════

CREATE TABLE "ProviderHealthChecks" (
    "ProviderName"  TEXT        PRIMARY KEY,
    "IsHealthy"     BOOLEAN     NOT NULL DEFAULT TRUE,
    "LatencyMs"     INTEGER,
    "ErrorMessage"  TEXT,
    "LastCheckedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "DownloadJobs" (
    "Id"           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "MediaItemId"  UUID        REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "SourceUrl"    TEXT        NOT NULL,
    "TargetPath"   TEXT        NOT NULL,
    "Status"       TEXT        NOT NULL DEFAULT 'Pending',
    -- Pending | Downloading | Completed | Failed | Cancelled
    "Progress"     DECIMAL(5,2) NOT NULL DEFAULT 0,
    "ErrorMessage" TEXT,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CompletedAt"  TIMESTAMPTZ
);

CREATE TABLE "StreamSessions" (
    "SessionId"    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "ProfileId"    UUID        NOT NULL REFERENCES "Profiles"("Id") ON DELETE CASCADE,
    "MediaItemId"  UUID        NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "SessionToken" TEXT        NOT NULL UNIQUE,
    "Mode"         TEXT        NOT NULL,   -- DirectPlay|Remux|Transcode
    "State"        TEXT        NOT NULL,   -- Active|Paused|Ended
    "PlaylistUrl"  TEXT        NOT NULL,
    "Width"        INTEGER,
    "Height"       INTEGER,
    "Bitrate"      BIGINT,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastActivityAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ExpiresAt"    TIMESTAMPTZ NOT NULL
);
```

---

## 4. Especificação de APIs e Endpoints

### 4.1 Convenções Gerais

```
Base URL: /api/v1
Auth:     Authorization: Bearer {jwt}
Errors:   { "error": "Code", "message": "Descrição", "details": {...} }
Paging:   { "items": [...], "total": N, "skip": 0, "take": 18 }
```

---

### 4.2 Gerenciamento de Bibliotecas

```
─────────────────────────────────────────────────────────────────
GET /api/v1/libraries
─────────────────────────────────────────────────────────────────
Response 200:
[
  {
    "id": "uuid",
    "name": "General",
    "kind": "General",
    "isSystem": true,
    "isEnabled": true,
    "folderCount": 1,
    "itemCount": 342,
    "lastScannedAt": "2026-04-29T10:00:00Z",
    "allowedExtensions": []
  }
]

─────────────────────────────────────────────────────────────────
POST /api/v1/libraries
─────────────────────────────────────────────────────────────────
Body:
{
  "name": "Filmes",
  "kind": "Video",
  "description": "Minha coleção de filmes",
  "folders": ["/media/filmes"],
  "allowedExtensions": [".mp4", ".mkv"],
  "preferredLanguage": "pt-BR",
  "preferredMetadataProvider": "Tmdb"
}

Response 201: LibraryDetailDto (com todos os campos)
Response 409: { "error": "Conflict", "message": "Library name already exists." }

─────────────────────────────────────────────────────────────────
PATCH /api/v1/libraries/{id}         ← RENOMEAR / RECONFIGURAR
─────────────────────────────────────────────────────────────────
Body (todos os campos opcionais — patch parcial):
{
  "name": "Meus Filmes",                       ← renomear
  "description": "Nova descrição",
  "isEnabled": true,
  "autoRefreshMetadata": true,
  "preferredLanguage": "pt-BR",
  "preferredMetadataProvider": "Tmdb",
  "allowedExtensions": [".mp4", ".mkv", ".avi"] ← alterar extensões
}

Response 200: LibraryDetailDto atualizado
Response 400: { "error": "Validation", "message": "Library name cannot be empty." }
Response 403: { "error": "Forbidden", "message": "System libraries cannot be deleted." }

─────────────────────────────────────────────────────────────────
DELETE /api/v1/libraries/{id}
─────────────────────────────────────────────────────────────────
Response 204: No Content
Response 422: { "error": "Unprocessable", "message": "Cannot delete system library." }

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GERENCIAMENTO DE PASTAS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

POST   /api/v1/libraries/{id}/folders
Body:  { "path": "/media/series" }
Response 201: LibraryDetailDto

PATCH  /api/v1/libraries/{id}/folders/{folderId}   ← ALTERAR DIRETÓRIO
Body:  { "path": "/media/movies", "isActive": true }
Response 200: LibraryDetailDto
Comportamento: mantém MediaItems existentes intactos, enfileira re-scan

DELETE /api/v1/libraries/{id}/folders/{folderId}
Response 204: No Content
Comportamento: não exclui MediaItems; eles ficam com FileStatus=FileNotFound

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
EXTENSÕES CONFIGURÁVEIS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PUT /api/v1/libraries/{id}/extensions
Body:  { "extensions": [".mp4", ".mkv", ".m4v"] }
       { "extensions": [] }  ← restaurar padrão do kind
Response 200: { "allowedExtensions": [".mp4", ".mkv", ".m4v"] }

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SCAN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

POST /api/v1/libraries/{id}/scan
Response 202: { "jobId": "uuid", "message": "Scan enqueued." }

GET /api/v1/libraries/{id}/scan/status
Response 200:
{
  "libraryId": "uuid",
  "isScanning": true,
  "lastScanAt": "2026-04-29T10:00:00Z",
  "lastResult": { "added": 12, "updated": 3, "removed": 0, "failed": 1, "elapsed": "00:00:45" }
}
```

---

### 4.3 Importação e Exportação

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
IMPORTAÇÃO EXTERNA (Discover → Library)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

POST /api/v1/discover/import
Body:
{
  "providerKind": "Tmdb",           // Tmdb | AniList | Gutenberg | LibriVox | MangaDex
  "externalId": "155",
  "mediaKind": "Video",
  "targetLibraryId": null           // null = auto-create "External Videos"
}

Response 201:
{
  "id": "uuid",
  "title": "The Dark Knight",
  "kind": "Video",
  "hasFile": false,
  "fileStatus": "ExternalOnly",
  "posterPath": "/images/uuid/poster.jpg",
  "libraryId": "uuid-da-lib-externa",
  "watchUrl": "/watch/uuid"
}

Response 409:
{
  "error": "Conflict",
  "message": "Item already imported.",
  "existingItemId": "uuid-existente"
}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
BUSCA NO DISCOVER
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

GET /api/v1/discover?q=batman&kind=Video&take=18&skip=0
Response 200:
{
  "items": [
    {
      "externalId": "155",
      "providerKind": "Tmdb",
      "title": "The Dark Knight",
      "year": 2008,
      "rating": 9.0,
      "overview": "...",
      "posterPath": "https://image.tmdb.org/t/p/w500/...",
      "genres": ["Action", "Crime"],
      "alreadyImported": false
    }
  ],
  "total": 42,
  "skip": 0,
  "take": 18
}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
EXPORTAÇÃO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

GET /api/v1/items/{id}/download
Response 200: octet-stream com headers:
  Content-Disposition: attachment; filename="The.Dark.Knight.2008.mkv"
  Content-Type: video/x-matroska
  Content-Length: 8589934592
Response 422: { "error": "NoLocalFile", "message": "Item has no local file. Use streaming." }

GET /api/v1/items/{id}/export?format=nfo
GET /api/v1/items/{id}/export?format=json
Response 200: arquivo correspondente

GET /api/v1/libraries/{id}/export?format=csv
GET /api/v1/libraries/{id}/export?format=json
Response 200: arquivo com todos os itens da biblioteca
```

---

### 4.4 Notificações e Recomendações

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
LISTAR NOTIFICAÇÕES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

GET /api/v1/notifications?unreadOnly=false&take=20&skip=0
Response 200:
{
  "items": [
    {
      "id": "uuid",
      "kind": "MediaAdded",
      "title": "The Dark Knight foi adicionado",
      "body": "Biblioteca: Demo · Filmes",
      "actionUrl": "/item/uuid",
      "imageUrl": "/images/uuid/poster.jpg",
      "isRead": false,
      "createdAt": "2026-04-29T10:00:00Z"
    },
    {
      "id": "uuid",
      "kind": "Recommendation",
      "title": "Recomendado para você",
      "body": "Inception — baseado em seus filmes favoritos",
      "actionUrl": "/item/uuid",
      "imageUrl": "https://...",
      "isRead": false,
      "createdAt": "2026-04-29T09:00:00Z"
    }
  ],
  "total": 5,
  "unreadCount": 3
}

GET /api/v1/notifications/unread-count
Response 200: { "count": 3 }

PATCH /api/v1/notifications/{id}/read
Response 204: No Content

PATCH /api/v1/notifications/read-all
Response 204: No Content

DELETE /api/v1/notifications/{id}
Response 204: No Content

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SSE — NOTIFICAÇÕES EM TEMPO REAL
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

GET /api/v1/notifications/stream
Headers: Accept: text/event-stream
Response: stream contínuo

  data: {"kind":"MediaAdded","title":"Inception foi adicionado","actionUrl":"/item/uuid"}

  data: {"kind":"ScanCompleted","title":"Scan concluído: +24 itens","actionUrl":"/settings#libraries"}

  : heartbeat (a cada 30s para manter conexão)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
RECOMENDAÇÕES (separado de notificações)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

GET /api/v1/profiles/{profileId}/recommendations?take=10
Response 200:
{
  "items": [ /* MediaItemDto array */ ],
  "computedAt": "2026-04-29T08:00:00Z"
}
```

---

### 4.5 Preferências de Idioma

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
IDIOMA DA INTERFACE (por User)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PATCH /api/v1/users/me
Body: { "preferredLanguage": "pt-BR" }
Response 200: UserDto atualizado
Efeito: Cookie MYTHRA_LOCALE=pt-BR setado; UI recarrega no idioma

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
IDIOMA DE CONTEÚDO (por Profile)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PATCH /api/v1/profiles/{id}/language
Body:
{
  "preferredContentLanguage":  "pt-BR",  // metadados e títulos
  "preferredSubtitleLanguage": "pt",     // legenda padrão no player
  "preferredAudioLanguage":    "pt",     // faixa de áudio padrão
  "showOriginalTitle":         false     // usar título localizado
}
Response 200: ProfileDto atualizado

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
METADADOS LOCALIZADOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

GET /api/v1/items/{id}?lang=pt-BR
Response 200:
{
  "id": "uuid",
  "title": "O Cavaleiro das Trevas",        ← título localizado se disponível
  "originalTitle": "The Dark Knight",        ← sempre retornado
  "overview": "Quando a ameaça conhecida...", ← sinopse localizada
  ...
}
```

---

### 4.6 Saúde dos Providers Externos

```
GET /api/v1/providers/health     [Role: Admin, Manager]
Response 200:
[
  { "providerName": "Vidsrc",    "isHealthy": true,  "latencyMs": 210  },
  { "providerName": "Gutenberg", "isHealthy": true,  "latencyMs": 380  },
  { "providerName": "LibriVox",  "isHealthy": false, "errorMessage": "Timeout after 5000ms" },
  { "providerName": "MangaDex",  "isHealthy": true,  "latencyMs": 145  }
]

POST /api/v1/providers/health/check   [Role: Admin]
Response 202: { "message": "Health check enqueued." }
```

---

## 5. Mapa de Navegação do Site

### 5.1 Estrutura Completa de Rotas

```
/                               ← Home
│   Hero Banner (5 itens recentes)
│   Continue Watching
│   Recently Added
│   Movies & TV
│   Anime · Manga · Books · Audiobooks
│   Recommended For You (NOVO)
│
├── /discover                   ← NOVO — busca em APIs externas
│     Tabs: Movies & TV | Anime | Manga | Books | Audiobooks
│     SearchBar com debounce
│     Grid de resultados com botão "Add" / "Watch Now"
│
├── /search                     ← Busca no catálogo local
│     Full-text search com filtros por kind e biblioteca
│
├── /notifications              ← NOVO — central de notificações
│     Agrupadas por: Hoje | Ontem | Esta semana | Anteriores
│     Tipos: MediaAdded | ScanCompleted | Recommendation | ...
│
├── /library/[id]               ← Conteúdo de uma biblioteca específica
│     Grid paginado de MediaItems
│     Filtros: kind, gênero, ano, rating, idioma
│
├── /library/all/[kind]         ← Todos os itens de um tipo
│     /library/all/Video
│     /library/all/Manga
│     /library/all/Book
│     /library/all/Audio
│
├── /item/[id]                  ← Página de detalhe (qualquer mídia)
│     Hero com poster e backdrop
│     Metadata completo: sinopse, cast, gêneros, studio
│     Botão de ação contextual:
│       Video → "Assistir" ou "Watch via {Provider}"
│       Book  → "Ler" ou "Links de leitura"
│       Manga → "Ler" ou "Ver no MangaDex"
│       Audio → "Ouvir"
│     Progresso do perfil atual
│
├── /watch/[id]                 ← Player de vídeo
│     VideoPlayer (HLS local) ou ExternalPlayer (iframe/mp4)
│     Barra de progresso salva automaticamente
│     Informações de streams, áudio e legendas
│
├── /read/[id]                  ← Reader EPUB/PDF/Manga
│     EPUB: viewer com paginação e ajuste de fonte
│     Manga: modo visor com direção configurável (LTR/RTL)
│
├── /listen/[id]                ← Player de áudio
│     Player com capítulos, velocidade de reprodução
│
└── /settings                   ← Configurações
      Layout: sidebar fixa + conteúdo à direita
      │
      ├── #libraries             ← Gerenciar bibliotecas
      │     Lista de bibliotecas com ações
      │     Modal de criação/edição (nome, pastas, extensões, provedor)
      │     Trigger de scan individual
      │
      ├── #profiles              ← Perfis de usuário
      │     Criar/editar/excluir perfis
      │     Avatar, modo infantil, kinds habilitados
      │
      ├── #account               ← Conta pessoal
      │     Email, username, senha
      │     Idioma da interface (dropdown UI language)
      │     Logout
      │
      ├── #playback              ← Preferências de reprodução
      │     Qualidade padrão de vídeo
      │     Velocidade padrão de áudio
      │     Salvar posição automaticamente
      │
      ├── #language              ← NOVO — idiomas de conteúdo
      │     Idioma preferido para metadados
      │     Idioma de legenda padrão
      │     Idioma de áudio padrão
      │     Mostrar título original (toggle)
      │
      ├── #metadata              ← Provedores de metadados
      │     TMDB API Key
      │     Prioridade de provedores (drag-and-drop)
      │
      ├── #notifications         ← NOVO — preferências de notificação
      │     Quais tipos de notificação receber
      │     Frequência de recomendações
      │     Email digest (futuro)
      │
      ├── #providers             ← NOVO — saúde dos providers externos
      │     Tabela com status, latência, última verificação
      │     Botão "Verificar agora"
      │
      └── #storage               ← NOVO — armazenamento e cache
            Uso de disco por biblioteca
            Limpar imagens em cache
            Gerenciar downloads pendentes
```

---

### 5.2 Fluxos de Usuário Principais

```
FLUXO 1 — Primeiro Uso
────────────────────────────────────────────────────────────
  /login
    │ credenciais válidas
    ▼
  / (Home — EmptyState)
    │ clica "Load demo content"
    ▼
  POST /api/v1/seed/demo
    │ seed concluído
    ▼
  / (Home — com filmes do demo)
    │ clica em um filme
    ▼
  /item/{id}
    │ clica "Assistir"
    ▼
  /watch/{id} com ExternalPlayer (Vidsrc iframe)

FLUXO 2 — Descoberta e Import
────────────────────────────────────────────────────────────
  /discover
    │ digita "Inception" na tab "Movies & TV"
    ▼
  GET /api/v1/discover?q=inception&kind=Video
    │ vê resultado: Inception (2010) ★8.8
    │ clica "▶ Watch Now"
    ▼
  POST /api/v1/discover/import
    │ item criado na biblioteca "External Videos"
    ▼
  /watch/{id} abre automaticamente
    │ ExternalPlayer carrega Vidsrc
    ▼
  Notificação "Inception importado" aparece no Bell icon

FLUXO 3 — Configurar Biblioteca
────────────────────────────────────────────────────────────
  /settings#libraries
    │ clica [Edit] na biblioteca "Filmes"
    ▼
  Modal de edição:
    · Renomeia para "Meus Filmes"
    · Adiciona pasta /media/movies
    · Configura extensões: .mp4, .mkv, .avi
    · Salva
    ▼
  PATCH /api/v1/libraries/{id} → sucesso
    │ clica [Scan]
    ▼
  POST /api/v1/libraries/{id}/scan → job enfileirado
    │ scan finaliza
    ▼
  Notificação "Scan concluído: +15 itens"

FLUXO 4 — Recomendações
────────────────────────────────────────────────────────────
  Usuário assiste The Dark Knight até o fim
    │ PlaybackProgress.IsCompleted = true
    ▼
  Job noturno: RecommendationEngine.ComputeAsync(profileId)
    │ Detecta gêneros favoritos: Action, Crime, Thriller
    │ Encontra itens com esses gêneros não assistidos
    │ Gera lista: [Inception, Se7en, Goodfellas]
    ▼
  Notification criada: "Recomendado: Inception"
    │ SSE push → Bell icon +1
    ▼
  /notifications → usuário vê recomendação → clica "Ver"
    ▼
  /item/{id da Inception}
```

---

### 5.3 Layout Base e Componentes de Shell

```
┌────────────────────────────────────────────────────────────────┐
│  TOPBAR (sticky, z-40)                                         │
│  [✦ Mythra] [Home][Discover][Movies][Manga][Books][Audiobooks] │
│                            [🔍][🔔³][Avatar ▼]                │
└────────────────────────────────────────────────────────────────┘
│
│  CONTENT AREA (PageScaffold: max-w-[1700px] mx-auto px-6 lg:px-10)
│
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  HeroBanner (apenas na Home — aspect-[21/9], full-width)  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ContentRow ("Continue Watching") ──────────────────────────── │
│  ContentRow ("Recently Added")    ──────────────────────────── │
│  ContentRow ("Recommended")       ──────────────────────────── │
│
└────────────────────────────────────────────────────────────────┘
```

---

## 6. Estratégia de Implementação Multi-idioma

### 6.1 Dois Domínios Distintos

```
┌─────────────────────────────────────────────────────────────────┐
│  DOMÍNIO 1: IDIOMA DA INTERFACE (UI)                             │
│                                                                   │
│  O quê:  Labels, botões, mensagens do sistema, navegação         │
│  Por quem: User.PreferredLanguage                                 │
│  Quando muda: Settings → Account → Language                      │
│  Mecanismo: Cookie MYTHRA_LOCALE + next-intl                     │
│  Suporte inicial: en (padrão) · pt-BR · es                       │
│  Futuramente: ja · zh · fr · de · ko                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  DOMÍNIO 2: IDIOMA DE CONTEÚDO (Mídia)                           │
│                                                                   │
│  O quê:  Título, sinopse, legendas padrão, faixa de áudio padrão │
│  Por quem: Profile.PreferredContentLanguage (por perfil)         │
│  Quando muda: Settings → Language (por perfil ativo)             │
│  Mecanismo: ?lang= query param na API / mapeamento no frontend   │
│  Suporte: qualquer idioma que TMDB/AniList suportam              │
└─────────────────────────────────────────────────────────────────┘
```

---

### 6.2 Implementação da Interface com next-intl

**Estrutura de arquivos:**
```
frontend/
├── messages/
│   ├── en.json             ← base (fallback obrigatório)
│   ├── pt-BR.json          ← Português Brasileiro
│   └── es.json             ← Espanhol
├── i18n/
│   ├── config.ts           ← locales suportados, defaultLocale
│   └── request.ts          ← resolve locale do cookie/header
└── middleware.ts           ← intercepta e seta locale
```

**`frontend/i18n/config.ts`:**
```typescript
export const locales = ['en', 'pt-BR', 'es'] as const;
export type Locale = typeof locales[number];
export const defaultLocale: Locale = 'en';
```

**`frontend/messages/pt-BR.json`:**
```json
{
  "nav": {
    "home": "Início",
    "discover": "Descobrir",
    "search": "Buscar",
    "movies": "Filmes & TV",
    "manga": "Mangá",
    "books": "Livros",
    "audiobooks": "Audiolivros",
    "notifications": "Notificações",
    "settings": "Configurações"
  },
  "home": {
    "continueWatching": "Continue assistindo",
    "recentlyAdded": "Adicionados recentemente",
    "freshInYourUniverse": "Novidades no seu universo",
    "recommendedForYou": "Recomendados para você",
    "yourUniverseAwaits": "Seu universo aguarda",
    "loadDemoContent": "Carregar conteúdo demo",
    "loading": "Carregando…",
    "demoLoaded": "Biblioteca demo carregada! Atualizando…"
  },
  "discover": {
    "title": "Descobrir",
    "subtitle": "Explore milhões de títulos de fontes externas",
    "searchPlaceholder": "Buscar filmes, séries, livros, mangás…",
    "tabs": {
      "moviesTV": "Filmes & TV",
      "anime": "Anime",
      "manga": "Mangá",
      "books": "Livros",
      "audiobooks": "Audiolivros"
    },
    "addToLibrary": "Adicionar",
    "watchNow": "Assistir agora",
    "alreadyAdded": "Na biblioteca",
    "noResults": "Nenhum resultado para \"{query}\".",
    "importSuccess": "{title} adicionado à sua biblioteca!"
  },
  "notifications": {
    "title": "Notificações",
    "markAllRead": "Marcar tudo como lido",
    "empty": "Nenhuma notificação.",
    "groupToday": "Hoje",
    "groupYesterday": "Ontem",
    "groupThisWeek": "Esta semana",
    "groupOlder": "Anteriores",
    "kinds": {
      "MediaAdded": "{title} foi adicionado",
      "ScanCompleted": "Scan concluído: +{count} itens",
      "ScanFailed": "Scan falhou na biblioteca {library}",
      "Recommendation": "Recomendado para você: {title}",
      "ImportCompleted": "{title} importado com sucesso",
      "ProviderUnhealthy": "Provider {name} com problemas"
    }
  },
  "settings": {
    "title": "Configurações",
    "subtitle": "Gerencie perfis, bibliotecas e preferências.",
    "sections": {
      "libraries": "Bibliotecas",
      "profiles": "Perfis",
      "account": "Conta",
      "playback": "Reprodução",
      "language": "Idioma",
      "metadata": "Metadados",
      "notifications": "Notificações",
      "providers": "Providers",
      "storage": "Armazenamento"
    },
    "libraries": {
      "newLibrary": "Nova biblioteca",
      "scan": "Escanear",
      "scanning": "Escaneando…",
      "lastScan": "Último scan: {date}",
      "notScanned": "Não escaneado",
      "noLibraries": "Nenhuma biblioteca. Crie uma para começar.",
      "editLibrary": "Editar biblioteca",
      "name": "Nome",
      "kind": "Tipo",
      "folders": "Pastas",
      "extensions": "Extensões (vazio = padrão do tipo)",
      "addFolder": "+ Adicionar pasta",
      "save": "Salvar",
      "cancel": "Cancelar",
      "delete": "Excluir biblioteca",
      "systemLibraryNotDeletable": "Bibliotecas do sistema não podem ser excluídas."
    },
    "language": {
      "uiLanguage": "Idioma da interface",
      "contentLanguage": "Idioma do conteúdo",
      "subtitleLanguage": "Legenda padrão",
      "audioLanguage": "Áudio padrão",
      "showOriginalTitle": "Mostrar título original",
      "saved": "Preferências de idioma salvas."
    },
    "account": {
      "signedInAs": "Conectado como {email}",
      "signOut": "Sair"
    }
  },
  "player": {
    "back": "Voltar",
    "streamingVia": "Streaming via {provider}",
    "streams": "Streams",
    "audio": "Áudio",
    "subtitles": "Legendas",
    "noSubtitles": "Sem legendas embarcadas",
    "viewFullDetails": "Ver detalhes completos →"
  },
  "errors": {
    "notFound": "Não encontrado",
    "noStream": "Nenhuma fonte de streaming disponível no momento.",
    "networkError": "Erro de rede. Tente novamente.",
    "unauthorized": "Sessão expirada. Faça login novamente."
  }
}
```

**Uso nos componentes React:**
```tsx
// Exemplo: Topbar.tsx com next-intl
import { useTranslations } from 'next-intl';

export function Topbar() {
  const t = useTranslations('nav');
  return (
    <nav>
      <NavLink href="/"          label={t('home')} />
      <NavLink href="/discover"  label={t('discover')} />
      <NavLink href="/search"    label={t('search')} />
    </nav>
  );
}

// Exemplo: Notifications com interpolação
const t = useTranslations('notifications.kinds');
t('MediaAdded', { title: 'Inception' })
// → "Inception foi adicionado"
```

---

### 6.3 Metadados de Mídia em Múltiplos Idiomas

```
FLUXO DE TÍTULO LOCALIZADO
══════════════════════════

  Requisição: GET /api/v1/items/{id}?lang=pt-BR
        │
        ▼
  MediaService.GetByIdAsync(id, language="pt-BR")
        │
        ├── Item tem LocalizedTitles["pt-BR"]?
        │     SIM → usar "O Cavaleiro das Trevas"
        │     NÃO → buscar no TMDB por translations
        │             ├── Encontrado → salvar em LocalizedTitles["pt-BR"]
        │             │              → retornar título localizado
        │             └── Não encontrado → fallback para Title original
        │
        └── Item tem LocalizedOverviews["pt-BR"]?
              SIM → usar sinopse em português
              NÃO → fallback para Overview original

  ARMAZENAMENTO:
  MediaItems.LocalizedTitles  = '{"pt-BR": "O Cavaleiro das Trevas", "es": "El Caballero Oscuro"}'
  MediaItems.LocalizedOverviews = '{"pt-BR": "Quando a ameaça conhecida como o Coringa..."}'


SELEÇÃO AUTOMÁTICA DE LEGENDA
══════════════════════════════

  Usuário abre /watch/{id}
  Profile.PreferredSubtitleLanguage = "pt"
        │
        ▼
  Subtitles filtradas por LanguageCode = "pt" ou "pt-BR"
        │
        ├── Encontrou? → selecionar automaticamente no player
        └── Não encontrou?
              ├── IsDefault=true existe? → selecionar esse
              └── Sem default → sem legenda (usuário escolhe manualmente)
```

---

### 6.4 Idiomas Suportados por Provedor de Metadados

| Provedor | Campo de Idioma | Ação |
|---|---|---|
| TMDB | `language=pt-BR` no request | Retorna título/sinopse localizados automaticamente |
| AniList | `title.romaji`, `title.native`, `title.english` | Mapeado pelo idioma preferido do perfil |
| Google Books | `volumeInfo.language` | Filtro na busca |
| Gutenberg | `languages[]` no JSON | Filtro por idioma na busca |
| LibriVox | `language` | Filtro na busca |
| MangaDex | `title` (multi-lang dict) | Seleciona pelo idioma preferido |

---

## 7. Roadmap Técnico Sugerido

### 7.1 Mapa de Dependências

```
Sprint 1: Fundação
├── Biblioteca General + IsSystem + AllowedExtensions
└── Settings UI funcional (sidebar + modal de edição)
            │
            ▼
Sprint 2: Notificações
├── Entidade Notification + SSE
└── Integração com ScanService
            │
            ▼
Sprint 3: Discover + Import          Sprint 4: Multi-idioma
├── DiscoverService                  ├── next-intl (pt-BR, en, es)
├── ImportService                    ├── Profile.Language fields
└── DiscoverController               └── API ?lang= param
            │                                    │
            └──────────────┬─────────────────────┘
                           ▼
Sprint 5: Recomendações + Provider Health
├── RecommendationEngine (genre-based)
├── ProviderHealthCheck jobs
└── Notifications de Recommendation
            │
            ▼
Sprint 6: Export + Import por URL
├── Download de arquivos
├── Export NFO/CSV
└── DownloadJob + progress
```

---

### 7.2 Sprints Detalhados

#### Sprint 1 — Fundação (Semanas 1-2)
**Objetivo:** Usuário consegue gerenciar bibliotecas completamente pela UI.

**Critério de aceite:** Modal de edição de biblioteca permite renomear, adicionar/remover pastas, configurar extensões e salvar sem quebrar dados existentes.

| Tarefa | Camada | Complexidade |
|---|---|---|
| `LibraryKind.General = 7` no enum | Domain | Baixa |
| `Library.IsSystem` + `AllowedExtensions` | Domain | Baixa |
| `GetEffectiveExtensions()` no Domain | Domain | Baixa |
| Migration EF Core com novos campos | Infrastructure | Baixa |
| `LibraryBootstrapService` | Application | Média |
| `PATCH /libraries/{id}/folders/{fid}` | Api | Média |
| `PUT /libraries/{id}/extensions` | Api | Baixa |
| Settings sidebar navegável (hash-based) | Frontend | Média |
| Modal de criação/edição de biblioteca | Frontend | Alta |
| `ExtensionsEditor` (tag input) | Frontend | Média |

---

#### Sprint 2 — Notificações (Semanas 3-4)
**Objetivo:** Usuário vê em tempo real quando um scan adiciona conteúdo.

**Critério de aceite:** Após scan, Bell icon mostra badge com número de novos itens. Clicar abre `/notifications` com cards descrevendo o que foi adicionado.

| Tarefa | Camada | Complexidade |
|---|---|---|
| Entidade `Notification` + migration | Domain/Infra | Baixa |
| `INotificationService` + implementação | Application | Média |
| Integrar em `ScanService` (MediaAdded, ScanCompleted) | Application | Média |
| Endpoints CRUD de notificações | Api | Baixa |
| SSE endpoint `/notifications/stream` | Api | Alta |
| Página `/notifications` | Frontend | Média |
| Hook `useNotifications()` com SSE | Frontend | Alta |
| Bell icon com badge numérico | Frontend | Baixa |

---

#### Sprint 3 — Discover + Import (Semanas 5-6)
**Objetivo:** Usuário busca "Inception" → clica "Watch Now" → assiste via Vidsrc.

**Critério de aceite:** Resultados de busca aparecem em < 1s. Import cria item na DB. Player abre automaticamente após import.

| Tarefa | Camada | Complexidade |
|---|---|---|
| `IDiscoverService` + `DiscoverService` | Application | Alta |
| `ImportService.ImportExternalAsync()` | Application | Alta |
| Auto-create bibliotecas "External {kind}" | Application | Média |
| `DiscoverController` (GET + POST) | Api | Média |
| Notificação `ImportCompleted` | Application | Baixa |
| Página `/discover` com tabs | Frontend | Alta |
| `DiscoverCard` com botões "Add"/"Watch Now" | Frontend | Média |
| Integrar `alreadyImported` na busca | Frontend | Média |
| Adicionar "Discover" ao Topbar | Frontend | Baixa |

---

#### Sprint 4 — Multi-idioma (Semanas 7-8)
**Objetivo:** Usuário seleciona PT-BR e toda a interface e metadados aparecem em português.

**Critério de aceite:** 100% dos strings da UI aparecem em pt-BR quando idioma está configurado. Títulos de filmes com tradução disponível no TMDB aparecem em português.

| Tarefa | Camada | Complexidade |
|---|---|---|
| Instalar e configurar `next-intl` | Frontend | Média |
| Criar `messages/en.json` (extrair todos os strings) | Frontend | Alta |
| Criar `messages/pt-BR.json` | Frontend | Alta |
| Criar `messages/es.json` | Frontend | Média |
| Seção Settings → Language | Frontend | Média |
| Cookie `MYTHRA_LOCALE` no authStore | Frontend | Baixa |
| `Profile.Preferred*Language` + migration | Domain/Infra | Baixa |
| `PATCH /profiles/{id}/language` | Api | Baixa |
| `MediaItems.LocalizedTitles` (JSONB) + migration | Domain/Infra | Média |
| `MediaService` com `?lang=` query param | Application | Média |
| Busca TMDB com idioma preferido do perfil | Infrastructure | Média |

---

#### Sprint 5 — Recomendações + Provider Health (Semanas 9-10)
**Objetivo:** Após assistir The Dark Knight, usuário recebe "Recomendado: Inception" nas notificações.

**Critério de aceite:** Job de recomendação roda diariamente. Resultados aparecem em seção dedicada na Home e como notificações.

| Tarefa | Camada | Complexidade |
|---|---|---|
| `IRecommendationService` + engine de gêneros | Application | Alta |
| Job periódico (24h) de recomendações | Infrastructure | Média |
| Endpoint `GET /profiles/{id}/recommendations` | Api | Baixa |
| `ProviderHealthCheck` + job a cada 6h | Infrastructure | Média |
| Endpoint `GET /providers/health` | Api | Baixa |
| Seção "Recommended For You" na Home | Frontend | Média |
| Seção Settings → Providers (tabela de saúde) | Frontend | Média |
| Notificações do tipo Recommendation | Frontend | Baixa |

---

#### Sprint 6 — Export + Completude (Semanas 11-12)
**Objetivo:** Usuário consegue baixar arquivos locais e exportar catálogos.

**Critério de aceite:** Download de arquivo local funciona com progress. Export de biblioteca gera CSV/JSON válido.

| Tarefa | Camada | Complexidade |
|---|---|---|
| `GET /items/{id}/download` (FileStreamResult) | Api | Média |
| `GET /items/{id}/export?format=nfo|json` | Api | Média |
| `GET /libraries/{id}/export?format=csv|json` | Api | Média |
| `DownloadJob` + worker em background | Infrastructure | Alta |
| `FileStatus` tracking em todos os flows | Application | Média |
| Botão "Download" na página de detalhe | Frontend | Baixa |
| Progress bar de download | Frontend | Média |
| Seção Settings → Storage | Frontend | Média |

---

### 7.3 Resumo Visual do Roadmap

```
  SEMANA  1   2   3   4   5   6   7   8   9  10  11  12
          │   │   │   │   │   │   │   │   │   │   │   │
Sprint 1  ████████                                       Fundação
Sprint 2          ████████                               Notificações
Sprint 3                  ████████                       Discover
Sprint 4                          ████████               Multi-idioma
Sprint 5                                  ████████       Recomendações
Sprint 6                                          ████████ Export

Legenda:  ██ = 2 semanas de desenvolvimento
```

---

### 7.4 Métricas de Sucesso por Sprint

| Sprint | KPI Principal | Meta |
|---|---|---|
| 1 — Fundação | Usuários conseguem reconfigurar biblioteca sem perder dados | 100% integridade |
| 2 — Notificações | Latência da notificação de scan | < 2 segundos |
| 3 — Discover | Tempo do clique "Watch Now" até player abrir | < 3 segundos |
| 4 — Multi-idioma | Cobertura de strings traduzidos em pt-BR | ≥ 95% |
| 5 — Recomendações | Relevância das recomendações (gênero match) | ≥ 70% |
| 6 — Export | Integridade do arquivo NFO exportado | Validação por Kodi |

---

### 7.5 Riscos Técnicos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Vidsrc muda estrutura de URL | Média | Alto | Config-driven URL, health checks, fallback para Consumet |
| TMDB bloqueia IP por rate limit | Baixa | Médio | Cache agressivo de metadados (30 dias), retry com backoff |
| Migration EF Core com dados em prod | Média | Alto | Sempre additive (ADD COLUMN com DEFAULT), nunca DROP sem backup |
| SSE não funciona atrás de proxy reverso (Nginx) | Alta | Médio | Adicionar `X-Accel-Buffering: no` no header, documentar config Nginx |
| next-intl incompatível com App Router | Baixa | Alto | Testar em branch isolada antes do Sprint 4, fallback para react-i18next |
| AllowedExtensions vazio quebra scan | Baixa | Médio | Lógica `GetEffectiveExtensions()` no Domain com padrão garantido |

---

*Documento gerado em 2026-04-29 · Mythra Media Server Project*  
*Revisão sugerida: início de cada sprint para ajuste de escopo*
