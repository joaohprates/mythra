namespace Mythra.Infrastructure.Addons;

public sealed class AddonOptions
{
    public const string SectionName = "Addons";

    /// <summary>Root directory scanned for addon sub-folders (each containing manifest.json).</summary>
    public string Directory { get; set; } = "addons";

    /// <summary>Default cache TTL applied when an addon does not specify one.</summary>
    public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How often the host health-checks all loaded addons.</summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
}
