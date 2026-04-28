using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mythra.Application.Services.Auth;
using Mythra.Application.Services.Libraries;
using Mythra.Application.Services.Media;
using Mythra.Application.Services.Progress;
using Mythra.Application.Services.Scanning;
using Mythra.Application.Services.Search;
using Mythra.Application.Services.Streaming;
using Mythra.Application.Services.SyncPlay;

namespace Mythra.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IStreamingService, StreamingService>();
        services.AddScoped<ISyncPlayService, SyncPlayService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IScanService, ScanService>();

        return services;
    }
}
