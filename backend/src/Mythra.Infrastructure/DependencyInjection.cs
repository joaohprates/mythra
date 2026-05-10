using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Mythra.Application.Abstractions.Addons;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Background;
using Mythra.Application.Abstractions.Files;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Providers;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Infrastructure.Addons;
using Mythra.Application.Abstractions.Streaming;
using Mythra.Application.Abstractions.Time;
using Mythra.Application.Services.ExternalProviders;
using Mythra.Infrastructure.Auth;
using Mythra.Infrastructure.Background;
using Mythra.Infrastructure.ExternalProviders;
using Mythra.Infrastructure.Files;
using Mythra.Infrastructure.Metadata;
using Mythra.Infrastructure.Persistence;
using Mythra.Infrastructure.Persistence.Repositories;
using Mythra.Infrastructure.Scanning;
using Mythra.Infrastructure.Streaming;
using Mythra.Infrastructure.Time;

namespace Mythra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<FfmpegOptions>(config.GetSection(FfmpegOptions.SectionName));
        services.Configure<MetadataOptions>(config.GetSection(MetadataOptions.SectionName));
        services.Configure<ExternalProvidersOptions>(config.GetSection(ExternalProvidersOptions.SectionName));

        // In-memory cache used by Discover, Cinemeta, and other infra services.
        services.AddMemoryCache();

        var connectionString = config.GetConnectionString("Default")
            ?? "Data Source=mythra.db;Cache=Shared;Foreign Keys=true;";
        services.AddDbContext<MythraDbContext>(opt =>
        {
            opt.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(typeof(MythraDbContext).Assembly.FullName));
        });
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MythraDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IMangaRepository, MangaRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IPlaybackProgressRepository, PlaybackProgressRepository>();
        services.AddScoped<IReadingProgressRepository, ReadingProgressRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IHighlightRepository, HighlightRepository>();
        services.AddScoped<IStreamSessionRepository, StreamSessionRepository>();
        services.AddScoped<ISyncRoomRepository, SyncRoomRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAddonRepository, AddonRepository>();
        services.AddScoped<IPlaylistRepository, PlaylistRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFileSystem, LocalFileSystem>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddSingleton<IMediaProbe, FfmpegMediaProbe>();
        services.AddSingleton<ITranscoder, FfmpegTranscoder>();

        var metaOpts = config.GetSection(MetadataOptions.SectionName).Get<MetadataOptions>() ?? new MetadataOptions();

        // Aggressive timeouts for upstream metadata HTTP calls — Discover should
        // never block longer than 8s on a single provider.
        services.AddHttpClient<TmdbMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.TmdbBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(8);
        }).ConfigurePrimaryHttpMessageHandler(BuildFastHandler).AddResilience();
        services.AddHttpClient<AniListMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.AniListBaseUrl ?? "https://graphql.anilist.co");
            c.Timeout = TimeSpan.FromSeconds(8);
        }).ConfigurePrimaryHttpMessageHandler(BuildFastHandler).AddResilience();
        services.AddHttpClient<OpenLibraryMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.OpenLibraryBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(8);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Mythra/0.1 (https://mythra.local)");
        }).ConfigurePrimaryHttpMessageHandler(BuildFastHandler).AddResilience();
        services.AddHttpClient<CinemetaMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.CinemetaBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(8);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Mythra/0.1 (https://mythra.local)");
        }).ConfigurePrimaryHttpMessageHandler(BuildFastHandler).AddResilience();
        services.AddHttpClient<GoogleBooksMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
            c.Timeout = TimeSpan.FromSeconds(8);
        }).AddResilience();

        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<CinemetaMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<TmdbMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<AniListMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<OpenLibraryMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<GoogleBooksMetadataProvider>());

        // Catalog browsing capability — TMDb + Cinemeta for video, AniList for anime/manga, OpenLibrary for books.
        services.AddSingleton<ICatalogProvider>(sp => sp.GetRequiredService<TmdbMetadataProvider>());
        services.AddSingleton<ICatalogProvider>(sp => sp.GetRequiredService<CinemetaMetadataProvider>());
        services.AddSingleton<ICatalogProvider, AniListCatalogProvider>();
        services.AddSingleton<ICatalogProvider>(sp => sp.GetRequiredService<OpenLibraryMetadataProvider>());

        services.AddSingleton<IMetadataProviderRegistry, MetadataProviderRegistry>();

        // Register each concrete scanner as itself so GeneralLibraryScanner can inject them directly,
        // then forward each as IMediaScanner for the registry's IEnumerable<IMediaScanner>.
        services.AddScoped<VideoLibraryScanner>();
        services.AddScoped<BookLibraryScanner>();
        services.AddScoped<MangaLibraryScanner>();
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<VideoLibraryScanner>());
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<BookLibraryScanner>());
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<MangaLibraryScanner>());
        services.AddScoped<IMediaScanner, GeneralLibraryScanner>();
        services.AddScoped<IMediaScannerRegistry, MediaScannerRegistry>();

        services.AddSingleton<IBackgroundJobQueue, ChannelBackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();
        services.AddHostedService<LibraryWatcherService>();

        // ── External content providers ──────────────────────────────────────────
        // Built-in pirate/streaming providers were removed; they will return as
        // user-installable addons. The IExternalVideoProvider / IExternalBookProvider
        // DI lists are intentionally empty by default. Addons may register implementations.

        // Orchestrator service (Application layer, needs scoped repo — register as Scoped)
        services.AddScoped<IExternalProviderService, ExternalProviderService>();

        // ── Addon system ────────────────────────────────────────────────────────

        // Named HttpClient used by OmdbMetadataProvider instances.
        services.AddHttpClient("OmdbMetadata", c => { c.Timeout = TimeSpan.FromSeconds(8); }).AddResilience();

        // Generic pool for the DLL-loading addon sandbox (AddonHost).
        services.AddHttpClient("AddonHttpClient", c => { c.Timeout = TimeSpan.FromSeconds(20); }).AddResilience();

        // Image proxy: lets the API rewrite/cache external poster URLs so the
        // frontend can render them even when the user's ISP blocks the CDN.
        services.AddHttpClient("ImageProxy", c => c.Timeout = TimeSpan.FromSeconds(10)).AddResilience();

        services.Configure<AddonOptions>(config.GetSection(AddonOptions.SectionName));

        // Runtime registries that addon-loaded providers register into.
        services.AddSingleton<IAddonStreamSourceRegistry, AddonStreamSourceRegistry>();
        services.AddSingleton<IAddonBookSourceRegistry,   AddonBookSourceRegistry>();

        // IAddonActivator: maps ProviderType strings to live provider instances.
        services.AddSingleton<IAddonActivator, AddonActivator>();

        // AddonActivationService: activates all Active addons from the DB on startup.
        services.AddHostedService<AddonActivationService>();

        // AddonHost: DLL-loading host for external .dll addons (advanced use).
        services.AddSingleton<AddonHost>();
        services.AddSingleton<IAddonHost>(sp => sp.GetRequiredService<AddonHost>());
        services.AddHostedService(sp => sp.GetRequiredService<AddonHost>());

        return services;
    }

    private static HttpMessageHandler BuildFastHandler() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectTimeout           = TimeSpan.FromSeconds(4),
        AutomaticDecompression   = DecompressionMethods.All,
    };

    /// <summary>
    /// Standard Polly resilience for every external HTTP call we make.
    /// Retries with jitter, attempt + total timeouts, and a circuit breaker.
    /// </summary>
    private static IHttpClientBuilder AddResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(o =>
        {
            o.Retry.MaxRetryAttempts = 3;
            o.Retry.UseJitter = true;
            o.Retry.Delay = TimeSpan.FromMilliseconds(400);
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(6);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            o.CircuitBreaker.FailureRatio = 0.5;
            o.CircuitBreaker.MinimumThroughput = 5;
        });
        return builder;
    }
}
