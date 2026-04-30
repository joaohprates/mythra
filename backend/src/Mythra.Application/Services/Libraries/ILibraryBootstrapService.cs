namespace Mythra.Application.Services.Libraries;

public interface ILibraryBootstrapService
{
    /// <summary>
    /// Ensures the system "General" library pointing to /media exists.
    /// Idempotent — safe to call on every startup.
    /// </summary>
    Task EnsureDefaultLibraryAsync(CancellationToken ct = default);
}
