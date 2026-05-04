namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Placeholder options object for the legacy "ExternalProviders" config section.
/// All built-in pirate/streaming providers were removed and will return as
/// user-installable addons; their toggles/URLs no longer live here. The class
/// is kept (and still bound) so any leftover keys in appsettings.json continue
/// to deserialize without errors.
/// </summary>
public sealed class ExternalProvidersOptions
{
    public const string SectionName = "ExternalProviders";
}
