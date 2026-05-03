using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Background;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Libraries;

namespace Mythra.Infrastructure.Scanning;

/// <summary>
/// Watches active library folders via FileSystemWatcher and enqueues
/// incremental scans with a debounce to avoid duplicate jobs.
/// </summary>
public sealed class LibraryWatcherService(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobQueue jobs,
    ILogger<LibraryWatcherService> log) : BackgroundService
{
    // libraryId → FileSystemWatcher list
    private readonly Dictionary<Guid, List<FileSystemWatcher>> _watchers = [];
    private readonly Dictionary<Guid, Timer> _debounceTimers = [];
    private readonly object _lock = new();
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeWatchersAsync(stoppingToken);

        // Re-sync watchers every 5 minutes (picks up newly added libraries)
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncWatchersAsync(stoppingToken);
        }
    }

    private async Task InitializeWatchersAsync(CancellationToken ct)
    {
        try
        {
            await SyncWatchersAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to initialize library watchers");
        }
    }

    private async Task SyncWatchersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ILibraryRepository>();
        var libraries = await repo.ListAsync(ct);

        var activeIds = new HashSet<Guid>();

        foreach (var library in libraries.Where(l => l.IsEnabled))
        {
            activeIds.Add(library.Id);
            var activeFolders = library.Folders
                .Where(f => f.IsActive && Directory.Exists(f.Path))
                .Select(f => f.Path)
                .ToList();

            lock (_lock)
            {
                if (!_watchers.ContainsKey(library.Id))
                    CreateWatchersForLibrary(library.Id, activeFolders);
            }
        }

        // Remove watchers for deleted/disabled libraries
        lock (_lock)
        {
            foreach (var id in _watchers.Keys.Except(activeIds).ToList())
                DisposeWatchersForLibrary(id);
        }
    }

    private void CreateWatchersForLibrary(Guid libraryId, IReadOnlyList<string> paths)
    {
        var watcherList = new List<FileSystemWatcher>();

        foreach (var path in paths)
        {
            try
            {
                var watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                };

                watcher.Created += (_, _) => OnChanged(libraryId);
                watcher.Deleted += (_, _) => OnChanged(libraryId);
                watcher.Renamed += (_, _) => OnChanged(libraryId);
                watcher.Error   += (_, e)  => log.LogWarning("FSW error for library {LibraryId}: {Error}", libraryId, e.GetException().Message);

                watcherList.Add(watcher);
                log.LogInformation("Watching library {LibraryId} at {Path}", libraryId, path);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Could not watch {Path} for library {LibraryId}", path, libraryId);
            }
        }

        _watchers[libraryId] = watcherList;
    }

    private void OnChanged(Guid libraryId)
    {
        lock (_lock)
        {
            if (_debounceTimers.TryGetValue(libraryId, out var existing))
            {
                existing.Change(DebounceDelay, Timeout.InfiniteTimeSpan);
                return;
            }

            _debounceTimers[libraryId] = new Timer(_ =>
            {
                lock (_lock) { _debounceTimers.Remove(libraryId); }
                EnqueueScan(libraryId);
            }, null, DebounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private void EnqueueScan(Guid libraryId)
    {
        log.LogInformation("FSW triggered scan for library {LibraryId}", libraryId);
        var job = new ScanLibraryJob(Guid.NewGuid(), libraryId);
        jobs.EnqueueAsync(job).AsTask().GetAwaiter().GetResult();
    }

    private void DisposeWatchersForLibrary(Guid libraryId)
    {
        if (_watchers.Remove(libraryId, out var list))
        {
            foreach (var w in list) w.Dispose();
        }
        if (_debounceTimers.Remove(libraryId, out var t))
        {
            t.Dispose();
        }
        log.LogInformation("Stopped watching library {LibraryId}", libraryId);
    }

    public override void Dispose()
    {
        lock (_lock)
        {
            foreach (var id in _watchers.Keys.ToList())
                DisposeWatchersForLibrary(id);
        }
        base.Dispose();
    }
}
