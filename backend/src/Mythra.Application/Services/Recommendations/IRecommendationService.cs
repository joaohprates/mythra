using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.Recommendations;

public sealed record RecommendationItemDto(
    Guid Id,
    MediaKind Kind,
    string Title,
    string? PosterPath,
    string? BackdropPath,
    double? Rating,
    int? Year,
    IReadOnlyList<string> Genres,
    string Reason);

public sealed record ProviderHealthDto(
    string Name,
    bool IsHealthy,
    string? ErrorMessage,
    DateTimeOffset CheckedAt);

public interface IRecommendationService
{
    /// <summary>Returns personalised recommendations for a profile based on genre similarity to recently consumed items.</summary>
    Task<Result<IReadOnlyList<RecommendationItemDto>>> GetForProfileAsync(
        Guid profileId, int take = 20, CancellationToken ct = default);

    /// <summary>Returns the current health status of all registered metadata providers.</summary>
    Task<Result<IReadOnlyList<ProviderHealthDto>>> GetProviderHealthAsync(CancellationToken ct = default);
}
