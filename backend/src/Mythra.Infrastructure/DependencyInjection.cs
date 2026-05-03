using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IAudioRepository, AudioRepository>();
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

        services.AddHttpClient<TmdbMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.TmdbBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<AniListMetadataProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<OpenLibraryMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.OpenLibraryBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Mythra/0.1 (https://mythra.local)");
        });
        services.AddHttpClient<CinemetaMetadataProvider>(c =>
        {
            c.BaseAddress = new Uri(metaOpts.CinemetaBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Mythra/0.1 (https://mythra.local)");
        });

        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<CinemetaMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<TmdbMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<AniListMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<OpenLibraryMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<SpotifyMetadataProvider>());
        services.AddSingleton<SpotifyMetadataProvider>();

        // Catalog browsing capability — Cinemeta is the primary, AniList augments anime catalogs.
        services.AddSingleton<ICatalogProvider>(sp => sp.GetRequiredService<CinemetaMetadataProvider>());
        services.AddSingleton<ICatalogProvider, AniListCatalogProvider>();

        services.AddSingleton<IMetadataProviderRegistry, MetadataProviderRegistry>();

        // Named HTTP clients for Spotify OAuth
        services.AddHttpClient("SpotifyAuth", c => { c.Timeout = TimeSpan.FromSeconds(15); });
        services.AddHttpClient("SpotifyApi", c => { c.Timeout = TimeSpan.FromSeconds(15); });

        // Register each concrete scanner as itself so GeneralLibraryScanner can inject them directly,
        // then forward each as IMediaScanner for the registry's IEnumerable<IMediaScanner>.
        services.AddScoped<VideoLibraryScanner>();
        services.AddScoped<BookLibraryScanner>();
        services.AddScoped<MangaLibraryScanner>();
        services.AddScoped<AudioLibraryScanner>();
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<VideoLibraryScanner>());
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<BookLibraryScanner>());
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<MangaLibraryScanner>());
        services.AddScoped<IMediaScanner>(sp => sp.GetRequiredService<AudioLibraryScanner>());
        services.AddScoped<IMediaScanner, GeneralLibraryScanner>();
        services.AddScoped<IMediaScannerRegistry, MediaScannerRegistry>();

        services.AddSingleton<IBackgroundJobQueue, ChannelBackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();
        services.AddHostedService<LibraryWatcherService>();

        // ── External content providers ──────────────────────────────────────────

        var extOpts = config.GetSection(ExternalProvidersOptions.SectionName)
                            .Get<ExternalProvidersOptions>() ?? new ExternalProvidersOptions();

        // Video providers (priority order: Videasy → Vidapi → Vidsrc → Consumet → ArchiveOrg)
        services.AddSingleton<VideasyProvider>();
        services.AddSingleton<IExternalVideoProvider>(sp => sp.GetRequiredService<VideasyProvider>());
        services.AddSingleton<VidapiProvider>();
        services.AddSingleton<IExternalVideoProvider>(sp => sp.GetRequiredService<VidapiProvider>());
        services.AddSingleton<VidsrcProvider>();
        services.AddSingleton<IExternalVideoProvider>(sp => sp.GetRequiredService<VidsrcProvider>());

        services.AddHttpClient<ConsumetProvider>(c =>
        {
            c.BaseAddress = new Uri(extOpts.ConsumetBaseUrl);
            c.Timeout     = TimeSpan.FromSeconds(20);
        });
        services.AddSingleton<IExternalVideoProvider>(sp => sp.GetRequiredService<ConsumetProvider>());

        services.AddHttpClient<ArchiveOrgProvider>(c =>
        {
            c.BaseAddress = new Uri(extOpts.ArchiveOrgBaseUrl);
            c.Timeout     = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IExternalVideoProvider>(sp => sp.GetRequiredService<ArchiveOrgProvider>());

        // Book / audio / manga providers
        services.AddHttpClient<GutenbergProvider>(c =>
        {
            c.BaseAddress = new Uri(extOpts.GutendexBaseUrl);
            c.Timeout     = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IExternalBookProvider>(sp => sp.GetRequiredService<GutenbergProvider>());

        services.AddHttpClient<LibriVoxProvider>(c =>
        {
            c.BaseAddress = new Uri(extOpts.LibriVoxBaseUrl);
            c.Timeout     = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IExternalBookProvider>(sp => sp.GetRequiredService<LibriVoxProvider>());

        services.AddHttpClient<MangaDexProvider>(c =>
        {
            c.BaseAddress = new Uri(extOpts.MangaDexBaseUrl);
            c.Timeout     = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Mythra/0.1 (https://mythra.local)");
        });
        services.AddSingleton<IExternalBookProvider>(sp => sp.GetRequiredService<MangaDexProvider>());

        // Orchestrator service (Application layer, needs scoped repo — register as Scoped)
        services.AddScoped<IExternalProviderService, ExternalProviderService>();

        // ── Addon system ────────────────────────────────────────────────────────
        services.AddMemoryCache();

        // Named HttpClient used by OmdbMetadataProvider instances.
        services.AddHttpClient("OmdbMetadata", c => { c.Timeout = TimeSpan.FromSeconds(15); });

        // Generic pool for the DLL-loading addon sandbox (AddonHost).
        services.AddHttpClient("AddonHttpClient", c => { c.Timeout = TimeSpan.FromSeconds(20); });

        services.Configure<AddonOptions>(config.GetSection(AddonOptions.SectionName));

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
}
