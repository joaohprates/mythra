using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Background;
using Mythra.Application.Services.Scanning;

namespace Mythra.Infrastructure.Background;

public sealed class BackgroundJobWorker(
    IBackgroundJobQueue queue,
    IServiceScopeFactory scopes,
    ILogger<BackgroundJobWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("BackgroundJobWorker started.");
        try
        {
            await foreach (var job in queue.DequeueAllAsync(stoppingToken))
            {
                _ = ProcessAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        log.LogInformation("BackgroundJobWorker stopped.");
    }

    private async Task ProcessAsync(BackgroundJob job, CancellationToken ct)
    {
        log.LogInformation("Job {JobId} ({Kind}) starting.", job.JobId, job.Kind);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            switch (job)
            {
                case ScanLibraryJob scan:
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IScanService>();
                    var result = await svc.RunAsync(scan.LibraryId, ct);
                    if (result.IsFailure) log.LogWarning("Scan failed: {Error}", result.Error);
                    break;
                }
                case RefreshMetadataJob:
                case GenerateThumbnailsJob:
                case CleanupTranscodeCacheJob:
                    log.LogDebug("Job {Kind} not yet implemented.", job.Kind);
                    break;
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Job {JobId} failed.", job.JobId);
        }
    }
}
