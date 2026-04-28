using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Background;
using Mythra.Application.Abstractions.Files;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Application.Abstractions.Streaming;
using Mythra.Application.Abstractions.Time;
using Mythra.Infrastructure.Auth;
using Mythra.Infrastructure.Background;
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

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFileSystem, LocalFileSystem>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddSingleton<IMediaProbe, FfmpegMediaProbe>();
        services.AddSingleton<ITranscoder, FfmpegTranscoder>();

        services.AddHttpClient<TmdbMetadataProvider>(c =>
        {
            var opts = config.GetSection(MetadataOptions.SectionName).Get<MetadataOptions>() ?? new MetadataOptions();
            c.BaseAddress = new Uri(opts.TmdbBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<AniListMetadataProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<MusicBrainzMetadataProvider>(c =>
        {
            var opts = config.GetSection(MetadataOptions.SectionName).Get<MetadataOptions>() ?? new MetadataOptions();
            c.BaseAddress = new Uri(opts.MusicBrainzBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(opts.MusicBrainzUserAgent);
        });
        services.AddHttpClient<GoogleBooksMetadataProvider>(c =>
        {
            var opts = config.GetSection(MetadataOptions.SectionName).Get<MetadataOptions>() ?? new MetadataOptions();
            c.BaseAddress = new Uri(opts.GoogleBooksBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<TmdbMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<AniListMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<MusicBrainzMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(sp => sp.GetRequiredService<GoogleBooksMetadataProvider>());
        services.AddSingleton<IMetadataProviderRegistry, MetadataProviderRegistry>();

        services.AddScoped<IMediaScanner, VideoLibraryScanner>();
        services.AddScoped<IMediaScanner, MangaLibraryScanner>();
        services.AddScoped<IMediaScanner, BookLibraryScanner>();
        services.AddScoped<IMediaScanner, AudioLibraryScanner>();
        services.AddScoped<IMediaScannerRegistry, MediaScannerRegistry>();

        services.AddSingleton<IBackgroundJobQueue, ChannelBackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();

        return services;
    }
}
