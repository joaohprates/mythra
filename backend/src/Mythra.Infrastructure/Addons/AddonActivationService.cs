using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Addons;
using Mythra.Application.Abstractions.Persistence;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Reads all Active addon entities from the database at startup and activates each one,
/// registering the corresponding providers into their respective registries.
///
/// This ensures that user-configured addons (imported via .mythra-addon.json and
/// configured with secrets) survive application restarts.
/// </summary>
public sealed class AddonActivationService(
    IServiceScopeFactory scopeFactory,
    IAddonActivator activator,
    ILogger<AddonActivationService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAddonRepository>();

        IReadOnlyList<Domain.Addons.Addon> active;
        try { active = await repo.ListActiveAsync(ct); }
        catch (Exception ex)
        {
            // DB might not be migrated yet on first run — don't crash the host.
            log.LogWarning(ex, "AddonActivationService: could not read addons from DB.");
            return;
        }

        foreach (var addon in active)
        {
            if (!activator.CanHandle(addon.ProviderType)) continue;
            try
            {
                activator.Activate(addon);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to activate addon '{Name}' ({Type}).", addon.Name, addon.ProviderType);
            }
        }

        log.LogInformation("AddonActivationService: activated {Count} addon(s).", active.Count(a => activator.CanHandle(a.ProviderType)));
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
