using Mythra.Addons.Contracts.Models;

namespace Mythra.Addons.Contracts;

/// <summary>
/// Base contract every Mythra addon must implement.
///
/// Lifecycle:
///   1. Host reads manifest.json and loads the assembly in an isolated AssemblyLoadContext.
///   2. Host instantiates the addon via Activator.CreateInstance (must have a public parameterless ctor).
///   3. Host calls InitializeAsync — addon wires up its context and validates required secrets.
///   4. Addon is registered in the appropriate registry (metadata, stream, subtitle).
///   5. On shutdown or disable: DisposeAsync is called and the ALC is unloaded.
/// </summary>
public interface IAddon : IAsyncDisposable
{
    /// <summary>Matches the "id" field in manifest.json.</summary>
    string Id { get; }

    string Name { get; }
    string Version { get; }

    /// <summary>
    /// Called once after the addon is loaded. Store the context and initialize resources.
    /// Throw if required secrets are missing or the configuration is invalid — this will
    /// move the addon to Disabled status and log the error.
    /// </summary>
    ValueTask InitializeAsync(IAddonContext context, CancellationToken ct = default);

    /// <summary>
    /// Called periodically by the host to verify the addon is still operational.
    /// A Degraded or Unhealthy result moves the addon to a warning/error state in the UI
    /// but does NOT unload it.
    /// </summary>
    ValueTask<AddonHealthStatus> HealthCheckAsync(CancellationToken ct = default);
}

public enum AddonHealthStatus { Healthy, Degraded, Unhealthy }
