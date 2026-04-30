# Mythra — Blueprint Técnico de Arquitetura
**Versão:** 1.0  
**Data:** 2026-04-29  
**Status:** Planejamento — não implementado

---

## Índice

1. [Visão Geral da Arquitetura](#1-visão-geral-da-arquitetura)
2. [Sistema de Biblioteca Local (Biblioteca Geral)](#2-sistema-de-biblioteca-local)
3. [Sistema de Importação e Exportação de Mídia](#3-importação-e-exportação-de-mídia)
4. [Gerenciamento Configurável de Bibliotecas](#4-gerenciamento-configurável-de-bibliotecas)
5. [Mapa de Páginas e Navegação](#5-mapa-de-páginas-e-navegação)
6. [Sistema de Notificações](#6-sistema-de-notificações)
7. [Sistema Multi-idioma](#7-sistema-multi-idioma)
8. [Estrutura de Banco de Dados](#8-estrutura-de-banco-de-dados)
9. [Especificação de APIs e Endpoints](#9-especificação-de-apis-e-endpoints)
10. [Roadmap Técnico](#10-roadmap-técnico)

---

## 1. Visão Geral da Arquitetura

### 1.1 Stack Atual

```
┌─────────────────────────────────────────────────────────┐
│                      FRONTEND                           │
│  Next.js 15 (App Router) · TypeScript · Tailwind CSS   │
│  TanStack Query · Framer Motion · Zustand (auth store)  │
└────────────────────┬────────────────────────────────────┘
                     │ HTTP / WebSocket (SignalR)
┌────────────────────▼────────────────────────────────────┐
│                     BACKEND (API)                       │
│              ASP.NET Core 10 · Controllers              │
│   JWT Auth · Error Middleware · Correlation IDs         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   APPLICATION LAYER                     │
│  Services · DTOs · Validators · Result<T>/Error pattern │
│  ILibraryService · IScanService · IStreamingService     │
│  IExternalProviderService · IDiscoverService (planned)  │
└────────────┬──────────────────────┬─────────────────────┘
             │                      │
┌────────────▼──────────┐  ┌───────▼─────────────────────┐
│    DOMAIN LAYER       │  │    INFRASTRUCTURE LAYER      │
│  Entities (EF Core)   │  │  EF Core + PostgreSQL/SQLite │
│  Library, MediaItem   │  │  LocalFileSystem, FFmpeg     │
│  VideoItem, BookItem  │  │  Metadata Providers (TMDB,   │
│  MangaItem, AudioItem │  │  AniList, GoogleBooks, etc.) │
│  Progress, SyncPlay   │  │  External Providers (Vidsrc, │
└───────────────────────┘  │  Gutenberg, LibriVox, etc.)  │
                           └─────────────────────────────-┘
```

### 1.2 Princípios Arquiteturais

| Princípio | Aplicação no Mythra |
|-----------|---------------------|
| Clean Architecture | Domain → Application → Infrastructure → Api (dependência apenas para dentro) |
| Provider Pattern | `IExternalVideoProvider`, `IExternalBookProvider` com prioridade e fallback |
| Result\<T\>/Error | Sem exceções no fluxo de negócio — erros explícitos e tipados |
| CQRS Leve | Services separados para leitura (`IMediaService`) e escrita (`IScanService`) |
| Idempotência | Scan, seed e import são seguros para reexecutar |

---

## 2. Sistema de Biblioteca Local

### 2.1 Conceito: Biblioteca Geral `/media`

O Mythra precisa de uma **biblioteca padrão ("General")** provisionada automaticamente no primeiro boot, mapeada para o diretório `/media`. Esta biblioteca é universal — aceita qualquer tipo de arquivo, com detecção automática de kind com base na extensão.

**Diferencial da Biblioteca Geral:**
- Não é tipada (kind = `General`, novo valor a adicionar ao enum)
- Não é deletável pelo usuário
- O scanner detecta automaticamente o tipo de cada arquivo encontrado
- Funciona como "caixa de entrada" para quem simplesmente joga arquivos na pasta

### 2.2 Diagrama do Sistema de Bibliotecas

```
  Boot do servidor
        │
        ▼
  Existe biblioteca "General"?
  ├── NÃO → Criar Library {Name="General", Kind=General, Folder="/media"}
  └── SIM → continuar
        │
        ▼
┌──────────────────────────────────────────────┐
│               BIBLIOTECA GERAL               │
│  Pasta: /media (container volume)            │
│  Kind:  General (auto-detect por extensão)   │
│  Scan:  automático na inicialização          │
└──────────┬───────────────────────────────────┘
           │ Encontrou arquivo
           ▼
  ┌─────────────────────────────────┐
  │    DETECTOR DE TIPO DE ARQUIVO  │
  │  .mp4 .mkv .avi → VideoItem     │
  │  .epub .pdf .mobi → BookItem    │
  │  .cbz .cbr → MangaItem          │
  │  .mp3 .flac .m4a → AudioItem    │
  │  .jpg .png .gif → ImageItem(*)  │
  │  outros → GenericFileItem(*)    │
  └─────────────────────────────────┘
  (*) novos tipos a adicionar

           │
           ▼
  Bibliotecas específicas (criadas pelo usuário)
  ├── Videos (Kind=Video)  → /media/filmes
  ├── Manga (Kind=Manga)   → /media/manga
  ├── Books (Kind=Book)    → /media/livros
  └── Music (Kind=Music)   → /media/musica
```

### 2.3 Auto-provisioning na Inicialização

**Backend — Novo serviço:** `ILibraryBootstrapService`

```csharp
// Mythra.Application/Services/Libraries/ILibraryBootstrapService.cs
public interface ILibraryBootstrapService
{
    /// <summary>
    /// Garante que a biblioteca "General" existe e aponta para /media.
    /// Idempotente — seguro para chamar a cada inicialização.
    /// </summary>
    Task EnsureDefaultLibraryAsync(CancellationToken ct = default);
}
```

**Integração no startup:**
```csharp
// Program.cs — após app.MapControllers()
await using var scope = app.Services.CreateAsyncScope();
var bootstrap = scope.ServiceProvider.GetRequiredService<ILibraryBootstrapService>();
await bootstrap.EnsureDefaultLibraryAsync();
```

### 2.4 Extensões Suportadas por Tipo

| Tipo | Extensões |
|------|-----------|
| Video | `.mp4` `.mkv` `.m4v` `.mov` `.avi` `.webm` `.ts` `.m2ts` |
| Book | `.epub` `.pdf` `.mobi` `.azw3` `.fb2` `.txt` |
| Manga | `.cbz` `.cbr` `.cb7` `.zip` com imagens |
| Audio | `.mp3` `.flac` `.m4a` `.ogg` `.wav` `.opus` `.aac` |
| Image | `.jpg` `.jpeg` `.png` `.gif` `.webp` `.heic` |

Estas extensões devem ser **configuráveis por biblioteca** (ver seção 4).

### 2.5 Mudanças no Domain

```csharp
// Mythra.Domain/Libraries/LibraryKind.cs — adicionar:
public enum LibraryKind
{
    Video    = 1,
    Anime    = 2,
    Manga    = 3,
    Book     = 4,
    Audiobook = 5,
    Music    = 6,
    General  = 7,   // NOVO — biblioteca universal
    Image    = 8,   // NOVO — galeria de fotos
}
```

```csharp
// Mythra.Domain/Libraries/Library.cs — adicionar:
public bool IsSystem { get; set; } = false;          // Não pode ser deletada
public List<string> AllowedExtensions { get; set; } = [];  // [] = usar padrão do kind
```

---

## 3. Importação e Exportação de Mídia

### 3.1 Fluxo de Importação

```
FONTES DE IMPORTAÇÃO
        │
        ├── 1. Arquivo local copiado para /media
        │         └── Scan periódico detecta → cria MediaItem
        │
        ├── 2. Upload via API (futuro)
        │         └── POST /api/v1/upload → salva em /media/uploads
        │
        ├── 3. Discover/Import externo (Vidsrc, Gutenberg, etc.)
        │         └── POST /api/v1/discover/import → MediaItem sem FilePath
        │
        └── 4. URL remota (futuro)
                  └── POST /api/v1/import/url → download em background

                         │
                         ▼
              ┌─────────────────────┐
              │    IMPORT PIPELINE  │
              │                     │
              │  1. Validação       │ ← tipo suportado? tamanho máx? duplicate?
              │  2. Metadados       │ ← busca TMDB/AniList/Gutenberg etc.
              │  3. Imagens         │ ← download poster/backdrop
              │  4. Indexação       │ ← cria MediaItem no banco
              │  5. Evento          │ ← dispara Notification "novo item"
              └─────────────────────┘
```

### 3.2 Fluxo de Exportação

```
EXPORTAÇÃO DE MÍDIA

Tipo 1 — Download do arquivo original
  GET /api/v1/items/{id}/download
  ├── Verifica permissão (HasFile=true)
  ├── Serve o arquivo diretamente (FileStreamResult)
  └── Header: Content-Disposition: attachment; filename="{title}.{ext}"

Tipo 2 — Export de metadados
  GET /api/v1/items/{id}/export?format=nfo|json|xml
  ├── Gera arquivo de metadados no formato solicitado
  └── NFO = formato Kodi-compatível (XML)

Tipo 3 — Exportação de biblioteca
  GET /api/v1/libraries/{id}/export?format=csv|json
  ├── Lista todos os itens com metadados
  └── Útil para migração / backup de catálogo

Tipo 4 — Streaming externo (já implementado)
  GET /api/v1/stream/external/{id}
  └── Retorna URL de iframe/HLS/MP4 via ExternalProviderService
```

### 3.3 Serviço de Importação

```csharp
// Mythra.Application/Services/Import/IImportService.cs
public interface IImportService
{
    /// <summary>Importa um item de fonte externa (Discover) sem arquivo local.</summary>
    Task<Result<MediaItemDto>> ImportExternalAsync(ImportExternalRequest req, CancellationToken ct = default);

    /// <summary>Importa um arquivo já presente em disco dentro de uma biblioteca.</summary>
    Task<Result<MediaItemDto>> ImportFileAsync(ImportFileRequest req, CancellationToken ct = default);

    /// <summary>Faz download de URL remota e importa (background job).</summary>
    Task<Result<Guid>> ImportFromUrlAsync(ImportUrlRequest req, CancellationToken ct = default);
}

public sealed record ImportExternalRequest(
    string Title,
    MediaKind Kind,
    string? ImdbId,
    string? TmdbId,
    string? AniListId,
    string? GutenbergId,
    string? LibriVoxId,
    string? MangaDexId,
    Guid? TargetLibraryId  // null = auto-escolher biblioteca External do kind
);

public sealed record ImportFileRequest(
    string FilePath,
    Guid LibraryId,
    bool FetchMetadata = true
);
```

### 3.4 Validação na Exportação de Fontes Externas

Para garantir que a exportação de mídia de fontes externas funciona:

```
Verificação de saúde dos providers (a cada 6 horas):
  GET /api/v1/stream/external/{id}
  ├── Vidsrc: testa URL de iframe com HEAD request → 200?
  ├── Gutenberg: testa URL de EPUB com HEAD → Content-Type: application/epub+zip?
  ├── LibriVox: testa zip URL → 200?
  └── MangaDex: testa API /manga → 200?

Resultado armazenado em:
  ProviderHealthStatus {
    ProviderName: string
    LastCheckedAt: DateTimeOffset
    IsHealthy: bool
    LatencyMs: int
    ErrorMessage?: string
  }

Exposto em:
  GET /api/v1/providers/health  (apenas Admin/Manager)
```

---

## 4. Gerenciamento Configurável de Bibliotecas

### 4.1 Capacidades da UI de Gerenciamento

A página `Settings → Libraries` deve permitir:

| Ação | Descrição | Implementação |
|------|-----------|---------------|
| Renomear biblioteca | Editar nome inline | `PATCH /api/v1/libraries/{id}` com `{name}` |
| Alterar pasta de origem | Adicionar/remover `LibraryFolder` | `POST/DELETE /api/v1/libraries/{id}/folders` |
| Alterar extensões permitidas | Lista de globs por biblioteca | Novo campo `AllowedExtensions` na entidade |
| Alterar provedor de metadados | Dropdown: TMDB, AniList, etc. | Campo `PreferredMetadataProvider` (já existe) |
| Ativar/desativar | Toggle isEnabled | `PATCH /api/v1/libraries/{id}` |
| Forçar re-scan | Reindexar arquivos | `POST /api/v1/libraries/{id}/scan` |
| Excluir biblioteca | Apenas não-sistema | `DELETE /api/v1/libraries/{id}` |
| Ver saúde dos providers | Status de cada fonte externa | `GET /api/v1/providers/health` |

### 4.2 Fluxo de Mudança de Diretório

```
Usuário altera pasta de /media/filmes para /media/movies
        │
        ▼
  PATCH /api/v1/libraries/{id}/folders/{folderId}
  { "path": "/media/movies" }
        │
        ▼
  LibraryService.UpdateFolderAsync()
  ├── Atualiza LibraryFolder.Path no banco
  ├── Mantém todos os MediaItems existentes intactos
  ├── Marca biblioteca como "pending rescan"
  └── Enfileira ScanLibraryJob
        │
        ▼
  ScanService executa em background
  ├── Varre /media/movies
  ├── Para cada arquivo: já existe pelo hash? → skip
  │                     não existe? → criar novo MediaItem
  ├── Arquivos que eram de /media/filmes e não encontra mais →
  │     marca como "FileNotFound" (não exclui automaticamente)
  └── Dispara evento LibraryScanned → Notification
```

### 4.3 Extensões Configuráveis por Biblioteca

```csharp
// Mythra.Domain/Libraries/Library.cs
/// <summary>
/// Extensões permitidas nesta biblioteca.
/// Lista vazia = usar padrão do Kind (ex: .mp4, .mkv para Video).
/// </summary>
public List<string> AllowedExtensions { get; set; } = [];

// Helper no Domain:
public IReadOnlyList<string> GetEffectiveExtensions()
{
    if (AllowedExtensions.Count > 0) return AllowedExtensions.AsReadOnly();
    return Kind switch
    {
        LibraryKind.Video or LibraryKind.Anime =>
            [".mp4", ".mkv", ".m4v", ".mov", ".avi", ".webm", ".ts", ".m2ts"],
        LibraryKind.Book =>
            [".epub", ".pdf", ".mobi", ".azw3", ".fb2"],
        LibraryKind.Manga =>
            [".cbz", ".cbr", ".cb7"],
        LibraryKind.Audiobook or LibraryKind.Music =>
            [".mp3", ".flac", ".m4a", ".ogg", ".wav", ".opus"],
        LibraryKind.General =>
            ["*"],  // aceita tudo
        _ => []
    };
}
```

**Na UI — componente de edição de extensões:**
```tsx
// TagInput para extensões
<ExtensionsEditor
  value={library.allowedExtensions}
  onChange={(exts) => updateLibrary({ allowedExtensions: exts })}
  placeholder="Deixe vazio para usar padrão do tipo"
/>
```

### 4.4 Integridade ao Renomear/Reconfigurar

- **Renomear:** apenas muda `Library.Name`, sem impacto nos `MediaItem`
- **Mudar pasta:** mantém `MediaItem` existentes; scan reconcilia (não deleta automaticamente)
- **Mudar extensões:** afeta apenas futuros scans; itens já indexados são mantidos
- **Desativar biblioteca:** oculta da UI mas mantém dados; scan é suspenso
- **Deletar biblioteca:** apenas se `IsSystem=false`; exclui `MediaItem` em cascata (configurável: "manter itens órfãos")

---

## 5. Mapa de Páginas e Navegação

### 5.1 Estrutura Completa de Rotas

```
/                          ← Home (hero banner + rows de conteúdo)
│
├── /discover              ← NOVO: Busca em APIs externas
│
├── /search                ← Busca no catálogo local
│
├── /notifications         ← NOVO: Central de notificações
│
├── /library
│   ├── /library/[id]      ← Biblioteca específica (grid paginado)
│   └── /library/all/[kind] ← Todos os itens de um kind
│
├── /item/[id]             ← Página de detalhe (qualquer mídia)
│
├── /watch/[id]            ← Player de vídeo (local ou externo)
├── /read/[id]             ← Reader de livros/manga
├── /listen/[id]           ← Player de áudio
│
└── /settings              ← Configurações (layout com sidebar)
    ├── /settings#libraries    ← Gerenciar bibliotecas
    ├── /settings#profiles     ← Perfis de usuário
    ├── /settings#account      ← Conta, senha, idioma
    ├── /settings#playback     ← Qualidade, legendas padrão
    ├── /settings#metadata     ← Provedores, idioma preferido
    ├── /settings#notifications ← Preferências de notificação
    ├── /settings#providers    ← NOVO: Status dos providers externos
    ├── /settings#language     ← NOVO: Interface e idioma de mídia
    └── /settings#storage      ← NOVO: Quotas, limpeza de cache
```

### 5.2 Navegação Global (Topbar)

**Estado atual:**
```
[Mythra logo] [Home] [Movies & TV] [Manga] [Books] [Audiobooks]
              [Search icon] [Bell icon] [Profile badge]
```

**Estado proposto:**
```
[Mythra logo] [Home] [Discover*] [Movies & TV] [Manga] [Books] [Audiobooks]
              [Search icon] [Bell icon (badge)*] [Profile badge]
```

`*` = novo elemento

### 5.3 Wireframe: Página Discover

```
┌──────────────────────────────────────────────────────────────────┐
│  Discover                                                         │
│  Explore millions of titles from external sources                │
├──────────────────────────────────────────────────────────────────┤
│  [🔍 Buscar filmes, séries, livros, mangás...        ] [Buscar]  │
│                                                                   │
│  [Movies & TV] [Anime] [Manga] [Books] [Audiobooks]  ← tabs     │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐        │
│  │     │  │     │  │     │  │     │  │     │  │     │        │
│  │POST │  │POST │  │POST │  │POST │  │POST │  │POST │        │
│  │     │  │     │  │     │  │     │  │     │  │     │        │
│  │Title│  │Title│  │Title│  │Title│  │Title│  │Title│        │
│  │Year │  │Year │  │Year │  │Year │  │Year │  │Year │        │
│  │★8.5 │  │★7.2 │  │★9.1 │  │★8.0 │  │★7.8 │  │★8.3 │        │
│  │[▶Add]│  │[▶Add]│  │[▶Add]│  │[▶Add]│  │[▶Add]│  │[▶Add]│        │
│  └─────┘  └─────┘  └─────┘  └─────┘  └─────┘  └─────┘        │
│                                                                   │
│                    [Carregar mais]                                │
└──────────────────────────────────────────────────────────────────┘

Botão [▶Add] expandido:
  ├── ▶ Watch Now  (adiciona + abre player imediatamente)
  └── + Add to Library (adiciona à biblioteca sem abrir)
```

### 5.4 Wireframe: Settings com Sidebar Funcional

```
Settings
┌────────────────┬──────────────────────────────────────────────┐
│ ◉ Libraries    │  Libraries                                    │
│   Profiles     │  ┌──────────────────────────────────────┐    │
│   Account      │  │ 📁 General     /media    [Scan][Edit] │    │
│   Playback     │  │ 🎬 Movies    /media/filmes [Scan][Edit]│   │
│   Metadata     │  │ 📚 Books    /media/books  [Scan][Edit] │   │
│   Notifications│  └──────────────────────────────────────┘    │
│   Providers    │  [+ New Library]                              │
│   Language     │                                               │
│   Storage      │  Edit Library Modal:                         │
│                │  ┌────────────────────────────────────────┐  │
│                │  │ Name: [Movies              ]           │  │
│                │  │ Kind: [Video ▼]                        │  │
│                │  │ Folders:                               │  │
│                │  │   /media/filmes  [✕]                   │  │
│                │  │   [+ Add folder]                       │  │
│                │  │ Extensions (vazio = padrão do tipo):   │  │
│                │  │   [.mp4] [.mkv] [+ adicionar]          │  │
│                │  │ Metadata Provider: [TMDB ▼]            │  │
│                │  │ Language: [Português ▼]                │  │
│                │  │ [Salvar]  [Cancelar]  [Excluir lib]    │  │
│                │  └────────────────────────────────────────┘  │
└────────────────┴──────────────────────────────────────────────┘
```

### 5.5 Wireframe: Página de Notificações

```
/notifications
┌────────────────────────────────────────────────────────┐
│  Notificações                              [Marcar tudo]│
├────────────────────────────────────────────────────────┤
│  Hoje                                                   │
│  ┌────────────────────────────────────────────────┐    │
│  │ 🎬 [POST] The Dark Knight foi adicionado       │    │
│  │         Biblioteca: Demo · há 2 horas     [Ver]│    │
│  └────────────────────────────────────────────────┘    │
│  ┌────────────────────────────────────────────────┐    │
│  │ ✨ Recomendado para você: Inception            │    │
│  │    Baseado em: The Dark Knight            [Ver]│    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  Ontem                                                  │
│  ┌────────────────────────────────────────────────┐    │
│  │ 📚 [POST] Scan concluído: 24 itens em Books    │    │
│  │         há 1 dia                          [Ver]│    │
│  └────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────┘
```

---

## 6. Sistema de Notificações

### 6.1 Tipos de Notificação

| Tipo | Gatilho | Destino |
|------|---------|---------|
| `MediaAdded` | Scan encontra item novo | Todos os perfis |
| `ScanCompleted` | Job de scan termina | Admin/Manager |
| `ScanFailed` | Job de scan falha | Admin/Manager |
| `MetadataRefreshed` | Metadados atualizados | Admin/Manager |
| `RecommendationReady` | Engine de recomendação calcula | Perfil específico |
| `ImportCompleted` | Import externo finaliza | Usuário que importou |
| `ProviderUnhealthy` | Provider externo com falha | Admin |

### 6.2 Modelo de Dados

```csharp
// Mythra.Domain/Notifications/Notification.cs
public sealed class Notification : Entity
{
    public Guid? ProfileId { get; set; }       // null = broadcast para todos
    public Guid? UserId { get; set; }
    public NotificationKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public string? ActionUrl { get; set; }     // ex: /item/{id}
    public string? ImageUrl { get; set; }      // poster do item
    public bool IsRead { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Payload { get; set; }       // JSON extra (mediaItemId, etc.)
}

public enum NotificationKind
{
    MediaAdded       = 1,
    ScanCompleted    = 2,
    ScanFailed       = 3,
    Recommendation   = 4,
    ImportCompleted  = 5,
    ProviderUnhealthy = 6,
    System           = 99,
}
```

### 6.3 Engine de Recomendação (Simples)

Para a primeira versão, uma engine baseada em **gêneros e tags** (sem ML):

```csharp
// Mythra.Application/Services/Recommendations/IRecommendationService.cs
public interface IRecommendationService
{
    /// <summary>
    /// Retorna IDs de itens recomendados para um perfil com base em:
    /// 1. Gêneros dos 10 últimos itens assistidos/lidos
    /// 2. Itens com rating > 7.5 nos mesmos gêneros
    /// 3. Exclui itens já consumidos pelo perfil
    /// </summary>
    Task<Result<IReadOnlyList<Guid>>> GetForProfileAsync(
        Guid profileId, int take = 10, CancellationToken ct = default);
}
```

**Algoritmo:**
```
1. Busca PlaybackProgress/ReadingProgress do perfil (últimos 30 dias)
2. Coleta gêneros dos MediaItems associados → conta frequência
3. Busca MediaItems com esses gêneros, rating ≥ 7.5, não no histórico
4. Ordena por: (frequência do gênero × rating) DESC
5. Limita a 10 resultados
6. Cria Notification.Recommendation para o perfil
```

### 6.4 Delivery de Notificações

```
CANAIS DE ENTREGA

1. In-App (Badge no Bell icon)
   GET /api/v1/notifications?unreadOnly=true
   PATCH /api/v1/notifications/{id}/read
   PATCH /api/v1/notifications/read-all

2. Server-Sent Events (SSE) — tempo real
   GET /api/v1/notifications/stream
   Content-Type: text/event-stream
   ← envia evento quando nova notification chega

3. Push Web (futuro)
   Via Service Worker + Web Push API
```

---

## 7. Sistema Multi-idioma

### 7.1 Dois Domínios de I18n

```
MULTI-IDIOMA NO MYTHRA

Domínio 1: INTERFACE (UI)
  Idioma da aplicação web
  Afeta: labels, mensagens, botões, textos do sistema
  Configurado por: usuário (por conta, não por perfil)
  Stored em: User.PreferredLanguage (já existe)

Domínio 2: MÍDIA (Conteúdo)
  Idioma preferido para metadados, legendas e áudio
  Afeta: título do item (idioma local vs original), sinopse,
         seleção padrão de legenda, seleção padrão de faixa de áudio
  Configurado por: perfil (por profile)
  Stored em: Profile.PreferredContentLanguage (novo campo)
             Library.PreferredLanguage (já existe)
```

### 7.2 Interface — Implementação com next-intl

**Stack escolhida:** `next-intl` (compatível com App Router do Next.js 15)

```
frontend/
├── messages/
│   ├── en.json         ← Inglês (padrão/fallback)
│   ├── pt-BR.json      ← Português Brasileiro
│   ├── es.json         ← Espanhol
│   ├── ja.json         ← Japonês (para usuários de anime)
│   └── zh.json         ← Chinês Simplificado
├── i18n.ts             ← configuração next-intl
└── middleware.ts       ← detecção de locale via URL ou cookie
```

**Estratégia de rota:** Locale no cookie/header (não na URL — evita quebrar rotas existentes):
```
Cookie: MYTHRA_LOCALE=pt-BR
Header: Accept-Language: pt-BR,pt;q=0.9,en;q=0.8
```

**Estrutura do arquivo de mensagens:**
```json
// messages/pt-BR.json
{
  "nav": {
    "home": "Início",
    "discover": "Descobrir",
    "search": "Buscar",
    "movies": "Filmes & TV",
    "manga": "Mangá",
    "books": "Livros",
    "audiobooks": "Audiolivros"
  },
  "settings": {
    "title": "Configurações",
    "sections": {
      "libraries": "Bibliotecas",
      "profiles": "Perfis",
      "account": "Conta",
      "language": "Idioma"
    }
  },
  "library": {
    "scan": "Escanear",
    "lastScan": "Último scan: {date}",
    "noItems": "Nenhum item encontrado."
  },
  "notifications": {
    "title": "Notificações",
    "markAllRead": "Marcar tudo como lido",
    "mediaAdded": "{title} foi adicionado à sua biblioteca",
    "recommendation": "Recomendado para você: {title}"
  },
  "player": {
    "back": "Voltar",
    "streamingVia": "Streaming via {provider}"
  }
}
```

### 7.3 Metadados de Mídia — Idioma Preferido

```csharp
// Mythra.Domain/Users/Profile.cs — adicionar:
public string? PreferredContentLanguage { get; set; }  // ex: "pt-BR", "en", "ja"
public bool ShowOriginalTitle { get; set; } = false;
public string? PreferredSubtitleLanguage { get; set; }
public string? PreferredAudioLanguage { get; set; }
```

**Lógica de exibição de título:**
```
Profile.ShowOriginalTitle = false AND MediaItem.Language ≠ Profile.PreferredContentLanguage
  → Buscar título localizado via TMDB (campo translations)
  → Cache em MediaItem.LocalizedTitles (JSONB dict: {"pt-BR": "O Cavaleiro das Trevas"})
  → Fallback: MediaItem.Title (título original)

Profile.ShowOriginalTitle = true
  → MediaItem.OriginalTitle ?? MediaItem.Title
```

**Seleção automática de legenda:**
```
Ao abrir /watch/[id]:
  1. Busca SubtitleTrack com LanguageCode == Profile.PreferredSubtitleLanguage
  2. Se não encontrado: busca SubtitleTrack com IsDefault = true
  3. Se não encontrado: sem legenda
```

### 7.4 Suporte a Idiomas nos Metadados

| Provedor | Campo de idioma | Ação |
|----------|----------------|------|
| TMDB | `language` no request, `translations` | Buscar título/sinopse no idioma preferido |
| AniList | `title.romaji`, `title.native`, `title.english` | Retornar campo conforme preferência |
| Google Books | `volumeInfo.language` | Filtrar por idioma |
| Gutenberg | `languages[]` | Filtrar por idioma |
| LibriVox | `language` | Filtrar por idioma |

### 7.5 Backend — Content-Language Header

```csharp
// Mythra.Api/Middleware/ContentLanguageMiddleware.cs
// Lê Accept-Language do request e coloca no contexto
// MediaService usa ICurrentLanguage para filtrar/ordenar títulos localizados
```

---

## 8. Estrutura de Banco de Dados

### 8.1 Tabelas Novas / Modificadas

```sql
-- MODIFICAÇÕES EM TABELAS EXISTENTES

-- Libraries: novos campos
ALTER TABLE "Libraries" ADD COLUMN "IsSystem" BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE "Libraries" ADD COLUMN "AllowedExtensions" TEXT[] NOT NULL DEFAULT '{}';
-- AllowedExtensions = array vazio → usa padrão do Kind

-- Profiles: novos campos de idioma
ALTER TABLE "Profiles" ADD COLUMN "PreferredContentLanguage" TEXT;
ALTER TABLE "Profiles" ADD COLUMN "ShowOriginalTitle" BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE "Profiles" ADD COLUMN "PreferredSubtitleLanguage" TEXT;
ALTER TABLE "Profiles" ADD COLUMN "PreferredAudioLanguage" TEXT;

-- MediaItems: suporte a títulos localizados
ALTER TABLE "MediaItems" ADD COLUMN "LocalizedTitles" JSONB;
-- ex: {"pt-BR": "O Poderoso Chefão", "es": "El Padrino"}

ALTER TABLE "MediaItems" ADD COLUMN "LocalizedOverviews" JSONB;
-- ex: {"pt-BR": "Sinopse em português..."}

-- Novos campos de provider
ALTER TABLE "MediaItems" ADD COLUMN "ProviderGutenbergId" TEXT;
ALTER TABLE "MediaItems" ADD COLUMN "ProviderLibriVoxId" TEXT;
ALTER TABLE "MediaItems" ADD COLUMN "ProviderMangaDexId" TEXT;
ALTER TABLE "MediaItems" ADD COLUMN "ProviderArchiveOrgId" TEXT;
ALTER TABLE "MediaItems" ADD COLUMN "FileStatus" TEXT NOT NULL DEFAULT 'Available';
-- FileStatus: Available | FileNotFound | ExternalOnly | Downloading

-- NOVAS TABELAS

-- Notificações
CREATE TABLE "Notifications" (
    "Id"         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"     UUID        REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ProfileId"  UUID        REFERENCES "Profiles"("Id") ON DELETE SET NULL,
    "Kind"       INTEGER     NOT NULL,
    "Title"      TEXT        NOT NULL,
    "Body"       TEXT,
    "ActionUrl"  TEXT,
    "ImageUrl"   TEXT,
    "IsRead"     BOOLEAN     NOT NULL DEFAULT FALSE,
    "Payload"    JSONB,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_notifications_user_unread
    ON "Notifications"("UserId", "IsRead") WHERE "IsRead" = FALSE;
CREATE INDEX idx_notifications_profile
    ON "Notifications"("ProfileId", "CreatedAt" DESC);

-- Saúde dos providers externos
CREATE TABLE "ProviderHealthChecks" (
    "Id"            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "ProviderName"  TEXT        NOT NULL UNIQUE,
    "IsHealthy"     BOOLEAN     NOT NULL DEFAULT TRUE,
    "LatencyMs"     INTEGER,
    "ErrorMessage"  TEXT,
    "LastCheckedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Fila de downloads (para import por URL)
CREATE TABLE "DownloadJobs" (
    "Id"          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "MediaItemId" UUID        REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "SourceUrl"   TEXT        NOT NULL,
    "TargetPath"  TEXT        NOT NULL,
    "Status"      TEXT        NOT NULL DEFAULT 'Pending',
    -- Status: Pending | Downloading | Completed | Failed
    "Progress"    DECIMAL(5,2),  -- 0.00 a 100.00
    "ErrorMessage" TEXT,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CompletedAt" TIMESTAMPTZ
);

-- Idiomas de conteúdo disponíveis (cache)
CREATE TABLE "MediaItemLanguages" (
    "MediaItemId"   UUID NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
    "LanguageCode"  TEXT NOT NULL,
    "Kind"          TEXT NOT NULL,  -- "audio" | "subtitle" | "metadata"
    PRIMARY KEY ("MediaItemId", "LanguageCode", "Kind")
);
```

### 8.2 Diagrama de Relacionamentos (simplificado)

```
Users ──< Profiles
│          │
│          ├──< PlaybackProgress ──> MediaItems
│          ├──< ReadingProgress  ──> MediaItems
│          └──< Notifications
│
├──< Sessions (JWT refresh)
└──< Notifications (user-level)

Libraries ──< LibraryFolders
     │
     └──< MediaItems (abstract)
               ├── VideoItems ──< AudioTracks
               │              ──< Subtitles
               │              ──< ChapterMarkers
               ├── BookItems  ──< BookChapters
               ├── MangaItems ──< MangaChapters
               └── AudioItems ──< AudioChapters

StreamSessions ──> MediaItems (HLS sessions)
DownloadJobs   ──> MediaItems
```

---

## 9. Especificação de APIs e Endpoints

### 9.1 Biblioteca Local e Gerenciamento

```
# Bibliotecas
GET    /api/v1/libraries                    → List<LibraryDto>
POST   /api/v1/libraries                    → LibraryDetailDto
GET    /api/v1/libraries/{id}               → LibraryDetailDto
PATCH  /api/v1/libraries/{id}               → LibraryDetailDto
DELETE /api/v1/libraries/{id}               → 204

# Pastas
POST   /api/v1/libraries/{id}/folders       → LibraryDetailDto
PATCH  /api/v1/libraries/{id}/folders/{fid} → LibraryDetailDto  ← NOVO
DELETE /api/v1/libraries/{id}/folders/{fid} → 204

# Extensões configuráveis
PUT    /api/v1/libraries/{id}/extensions    → LibraryDetailDto  ← NOVO
       Body: { "extensions": [".mp4", ".mkv"] }

# Scan
POST   /api/v1/libraries/{id}/scan         → { "jobId": "..." }
GET    /api/v1/libraries/{id}/scan/status  → ScanStatusDto      ← NOVO
```

### 9.2 Importação e Exportação

```
# Importação
POST   /api/v1/import/external             → MediaItemDto
       Body: ImportExternalRequest
POST   /api/v1/import/url                  → { "jobId": "..." }
       Body: { "url": "...", "libraryId": "..." }
GET    /api/v1/import/jobs/{jobId}         → DownloadJobDto      ← NOVO

# Exportação
GET    /api/v1/items/{id}/download         → FileStreamResult
GET    /api/v1/items/{id}/export           → arquivo (NFO/JSON/XML)
       ?format=nfo|json|xml
GET    /api/v1/libraries/{id}/export       → arquivo (CSV/JSON)
       ?format=csv|json
```

### 9.3 Notificações

```
# CRUD de notificações
GET    /api/v1/notifications               → PagedResult<NotificationDto>
       ?unreadOnly=true&take=20&skip=0
PATCH  /api/v1/notifications/{id}/read    → 204
PATCH  /api/v1/notifications/read-all     → 204
DELETE /api/v1/notifications/{id}         → 204

# SSE (tempo real)
GET    /api/v1/notifications/stream        → text/event-stream
```

### 9.4 Saúde dos Providers

```
GET    /api/v1/providers/health            → List<ProviderHealthDto>
POST   /api/v1/providers/health/check      → 202 (dispara verificação) [Admin]
```

### 9.5 Multi-idioma e Perfil

```
# Preferências de idioma do perfil
PATCH  /api/v1/profiles/{id}/language      → ProfileDto   ← NOVO
       Body: {
         "preferredContentLanguage": "pt-BR",
         "preferredSubtitleLanguage": "pt",
         "preferredAudioLanguage": "pt",
         "showOriginalTitle": false
       }

# Metadados localizados
GET    /api/v1/items/{id}                  → MediaItemDto
       ?lang=pt-BR  ← retorna título/sinopse no idioma se disponível
```

### 9.6 Discover (já planejado, incluído por completude)

```
GET    /api/v1/discover                    → PagedResult<DiscoverResultDto>
       ?q=batman&kind=Video&take=18&skip=0
POST   /api/v1/discover/import             → MediaItemDto
       Body: { "providerKind": "Tmdb", "externalId": "155", "mediaKind": "Video" }
```

---

## 10. Roadmap Técnico

### 10.1 Critérios de Priorização

| Critério | Peso |
|----------|------|
| Impacto direto na experiência do usuário | Alto |
| Pré-requisito para outras features | Alto |
| Complexidade de implementação | Moderado |
| Risco técnico | Moderado |

### 10.2 Roadmap por Sprint (2 semanas cada)

---

#### 🏃 Sprint 1 — Fundação: Biblioteca Geral e Settings Funcional
**Por quê primeiro:** Sem biblioteca default e UI de settings funcional, todas as outras features ficam bloqueadas ou difíceis de testar.

**Backend:**
- [ ] Adicionar `LibraryKind.General` ao enum
- [ ] Adicionar `IsSystem`, `AllowedExtensions` à entidade `Library`
- [ ] Criar `LibraryBootstrapService` (provisiona `/media` no boot)
- [ ] Atualizar `GetEffectiveExtensions()` no Domain
- [ ] `PATCH /api/v1/libraries/{id}/folders/{fid}` (muda path de pasta)
- [ ] `PUT /api/v1/libraries/{id}/extensions` (configura extensões)
- [ ] Migration EF Core com novos campos

**Frontend:**
- [ ] Transformar Settings em layout com sidebar navegável (hash-based)
- [ ] Implementar todas as seções como componentes separados
- [ ] Modal de edição de biblioteca (nome, pastas, extensões, provedor, idioma)
- [ ] Componente `ExtensionsEditor` (tag input)

**Resultado:** Usuário consegue criar e configurar bibliotecas completamente pela UI.

---

#### 🏃 Sprint 2 — Notificações: Backend + UI
**Por quê segundo:** Notificações são consumidas por várias features futuras (scan, import, recomendações). Melhor construir a infraestrutura cedo.

**Backend:**
- [ ] Entidade `Notification` + migration
- [ ] `INotificationService` + `NotificationService`
- [ ] Integrar em `ScanService` (dispara `MediaAdded` e `ScanCompleted`)
- [ ] CRUD de notificações (`GET`, `PATCH read`, `DELETE`)
- [ ] SSE endpoint (`/api/v1/notifications/stream`)

**Frontend:**
- [ ] Página `/notifications`
- [ ] Bell icon com badge de não-lidas
- [ ] Hook `useNotifications()` com polling/SSE
- [ ] Cards de notificação por tipo (MediaAdded, ScanCompleted, etc.)

**Resultado:** Usuário vê em tempo real quando um scan adiciona conteúdo.

---

#### 🏃 Sprint 3 — Discover: Busca e Import de Fontes Externas
**Por quê terceiro:** É a feature mais solicitada pelo usuário. Depende de bibliotecas configuradas (Sprint 1) e notificações de import (Sprint 2).

**Backend:**
- [ ] `IDiscoverService` + `DiscoverService` (usa `IMetadataProviderRegistry`)
- [ ] `DiscoverController` (`GET /discover`, `POST /discover/import`)
- [ ] `ImportService.ImportExternalAsync()` (cria MediaItem sem FilePath)
- [ ] Auto-create bibliotecas "External" por kind
- [ ] Notificação `ImportCompleted` após import

**Frontend:**
- [ ] Página `/discover` com tabs por kind
- [ ] SearchBar com debounce 400ms
- [ ] Grid de resultados com `DiscoverCard` (poster, título, ano, rating)
- [ ] Botões "Watch Now" / "Add to Library" com feedback visual
- [ ] Adicionar "Discover" ao Topbar

**Resultado:** Usuário busca "Inception" → clica "Watch Now" → assiste via Vidsrc em segundos.

---

#### 🏃 Sprint 4 — Multi-idioma: Interface PT-BR + EN
**Por quê quarto:** Com as features core funcionando, adicionar i18n sem quebrar nada já existente.

**Frontend:**
- [ ] Instalar e configurar `next-intl`
- [ ] Criar `messages/en.json` (extrair todos os strings hardcoded)
- [ ] Criar `messages/pt-BR.json` (tradução completa)
- [ ] Seção Settings → Language (seletor de idioma da interface)
- [ ] Cookie `MYTHRA_LOCALE` persistido no authStore

**Backend:**
- [ ] Adicionar campos de idioma ao `Profile` + migration
- [ ] `PATCH /api/v1/profiles/{id}/language`
- [ ] `MediaService` respeita `?lang=` query param
- [ ] Busca de metadados TMDB com idioma preferido do perfil

**Resultado:** Usuário seleciona PT-BR nas configurações; toda a interface e metadados aparecem em português.

---

#### 🏃 Sprint 5 — Recomendações + Provider Health
**Por quê quinto:** Feature de valor alto mas que depende de histórico acumulado (Progress) e de toda a infraestrutura de notificações.

**Backend:**
- [ ] `IRecommendationService` com algoritmo de gêneros
- [ ] Job periódico (a cada 24h) que gera recomendações por perfil
- [ ] Notificações `Recommendation` para cada perfil
- [ ] Entidade `ProviderHealthCheck` + verificação periódica a cada 6h
- [ ] `GET /api/v1/providers/health`

**Frontend:**
- [ ] Seção "Recommended for You" na Home page
- [ ] Seção Settings → Providers (status com ícones de saúde)
- [ ] Cards de notificação do tipo Recommendation com poster

**Resultado:** Após assistir The Dark Knight, o usuário recebe "Recomendado: Inception" na área de notificações.

---

#### 🏃 Sprint 6 — Export + Importação por URL (Qualidade e Completude)
**Por quê por último:** Features de exportação e download em background têm menor urgência e maior complexidade de edge cases.

**Backend:**
- [ ] `GET /api/v1/items/{id}/download` (serve arquivo)
- [ ] `GET /api/v1/items/{id}/export?format=nfo|json`
- [ ] `GET /api/v1/libraries/{id}/export?format=csv|json`
- [ ] `POST /api/v1/import/url` com `DownloadJob`
- [ ] Worker de download em background com progress report
- [ ] `FileStatus` tracking (`Available | FileNotFound | ExternalOnly | Downloading`)

**Frontend:**
- [ ] Botão "Download" na página de detalhe (para itens com arquivo)
- [ ] Indicador de progresso de download
- [ ] Export de catálogo em Settings → Storage

---

### 10.3 Resumo do Roadmap

```
Sprint 1 │▓▓▓▓▓▓▓▓│ Biblioteca Geral + Settings  ← BASE
Sprint 2 │▓▓▓▓▓▓▓▓│ Notificações                 ← INFRAESTRUTURA
Sprint 3 │▓▓▓▓▓▓▓▓│ Discover + Import             ← FEATURE CORE
Sprint 4 │▓▓▓▓▓▓▓▓│ Multi-idioma                  ← UX POLISH
Sprint 5 │▓▓▓▓▓▓▓▓│ Recomendações + Health         ← VALOR ADICIONADO
Sprint 6 │▓▓▓▓▓▓▓▓│ Export + Import por URL        ← COMPLETUDE
         └────────┴──────────────────────────────► ~12 semanas
```

### 10.4 Dependências entre Features

```
Biblioteca Geral (Sprint 1)
    └─► Notificações de Scan (Sprint 2)
            └─► Recomendações (Sprint 5)

Settings Funcional (Sprint 1)
    └─► Idioma de Biblioteca (Sprint 4)

Discover + Import (Sprint 3)
    └─► Notificações de Import (depende Sprint 2)
    └─► Idioma preferido nos resultados (depende Sprint 4)

Export (Sprint 6)
    └─► Depende de MediaItem.FileStatus (Sprint 1/3)
```

---

## Apêndice A — Convenções de Nomenclatura

| Camada | Convenção |
|--------|-----------|
| Domain entities | PascalCase, sem sufixo (`Library`, `VideoItem`) |
| DTOs | sufixo `Dto` ou `Request` (`LibraryDto`, `CreateLibraryRequest`) |
| Services | prefixo `I` para interface, sem prefixo para impl (`ILibraryService`, `LibraryService`) |
| Controllers | sufixo `Controller`, rota: `api/v1/{resource}` em kebab-case |
| Frontend pages | `page.tsx` em `src/app/{route}/` |
| Frontend components | PascalCase em `src/components/{category}/` |
| i18n keys | camelCase aninhado: `nav.home`, `settings.sections.libraries` |

## Apêndice B — Variáveis de Ambiente Necessárias

```bash
# Backend — appsettings.json ou environment
ConnectionStrings__DefaultConnection=...
Jwt__SecretKey=...
Jwt__Issuer=mythra
Jwt__Audience=mythra-client

# Providers de metadados (já existentes)
MetadataOptions__TmdbApiKey=...
MetadataOptions__GoogleBooksApiKey=...

# Providers externos (já existentes)
ExternalProviders__VidsrcEnabled=true
ExternalProviders__VidsrcBaseUrl=https://vidsrc.xyz/embed
ExternalProviders__ConsumetEnabled=true
ExternalProviders__ConsumetBaseUrl=https://consumet.your-instance.com
ExternalProviders__GutendexEnabled=true
ExternalProviders__GutendexBaseUrl=https://gutendex.com
ExternalProviders__LibriVoxEnabled=true
ExternalProviders__MangaDexEnabled=true

# NOVO — Notificações
Notifications__RecommendationJobIntervalHours=24
Notifications__ProviderHealthCheckIntervalHours=6
Notifications__MaxPerUserUnread=100

# NOVO — i18n
App__DefaultLocale=en
App__SupportedLocales=en,pt-BR,es,ja,zh

# Frontend (.env.local)
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_DEFAULT_LOCALE=en
```

---

*Documento criado como blueprint para desenvolvimento incremental do Mythra.*  
*Cada sprint representa ~2 semanas de desenvolvimento de uma pessoa.*  
*Revisar e ajustar estimativas conforme o progresso real.*
