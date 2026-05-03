namespace Mythra.Addons.Contracts.Models;

/// <summary>
/// Capabilities that an addon can advertise in its manifest.
/// An addon may implement multiple capabilities (e.g. metadata + scrobbling).
/// </summary>
public enum AddonCapability
{
    MetadataProvider,
    StreamSource,
    SubtitleProvider,
    ScrobblingService,
    Notification,
}
