using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Playlists;
using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;
using Mythra.Domain.Playlists;

namespace Mythra.Application.Services.Playlists;

public sealed class PlaylistService(
    IPlaylistRepository playlists,
    IMediaItemRepository media,
    IProfileRepository profiles,
    IUnitOfWork uow,
    ILogger<PlaylistService> log) : IPlaylistService
{
    public async Task<Result<IReadOnlyList<PlaylistDto>>> ListAsync(Guid profileId, CancellationToken ct = default)
    {
        var list = await playlists.ListByProfileAsync(profileId, ct);
        return Result<IReadOnlyList<PlaylistDto>>.Success(list.Select(ToDto).ToList());
    }

    public async Task<Result<PlaylistDetailDto>> GetAsync(Guid id, Guid profileId, CancellationToken ct = default)
    {
        var playlist = await playlists.GetWithItemsAsync(id, ct);
        if (playlist is null) return Error.NotFound("Playlist", id);
        if (playlist.ProfileId != profileId && !playlist.IsPublic) return Error.Forbidden();
        return await ToDetailAsync(playlist, ct);
    }

    public async Task<Result<PlaylistDetailDto>> CreateAsync(Guid profileId, CreatePlaylistRequest req, CancellationToken ct = default)
    {
        if (profileId == Guid.Empty) return Error.Validation("Active profile is required.");
        if (string.IsNullOrWhiteSpace(req.Name))
            return Error.Validation("Playlist name cannot be empty.");

        var profile = await profiles.GetByIdAsync(profileId, ct);
        if (profile is null) return Error.NotFound("Profile", profileId);

        try
        {
            var playlist = new Playlist(profileId, req.Name, req.Description, req.IsPublic);
            await playlists.AddAsync(playlist, ct);
            await uow.SaveChangesAsync(ct);
            log.LogInformation("Profile {ProfileId} created playlist {Name}", profileId, playlist.Name);
            return await ToDetailAsync(playlist, ct);
        }
        catch (InvariantViolationException ex)
        {
            return Error.Validation(ex.Message);
        }
    }

    public async Task<Result<PlaylistDetailDto>> UpdateAsync(Guid id, Guid profileId, UpdatePlaylistRequest req, CancellationToken ct = default)
    {
        var playlist = await playlists.GetWithItemsAsync(id, ct);
        if (playlist is null) return Error.NotFound("Playlist", id);
        if (playlist.ProfileId != profileId) return Error.Forbidden();

        if (req.Name is not null || req.Description is not null)
            playlist.Rename(req.Name ?? playlist.Name, req.Description ?? playlist.Description);

        if (req.IsPublic.HasValue)
            playlist.SetVisibility(req.IsPublic.Value);

        await uow.SaveChangesAsync(ct);
        return await ToDetailAsync(playlist, ct);
    }

    public async Task<Result<PlaylistDetailDto>> AddItemAsync(Guid id, Guid profileId, AddPlaylistItemRequest req, CancellationToken ct = default)
    {
        var playlist = await playlists.GetWithItemsAsync(id, ct);
        if (playlist is null) return Error.NotFound("Playlist", id);
        if (playlist.ProfileId != profileId) return Error.Forbidden();

        var mediaItem = await media.GetByIdAsync(req.MediaItemId, ct);
        if (mediaItem is null) return Error.NotFound("MediaItem", req.MediaItemId);

        playlist.AddItem(req.MediaItemId);
        await uow.SaveChangesAsync(ct);
        return await ToDetailAsync(playlist, ct);
    }

    public async Task<Result<PlaylistDetailDto>> RemoveItemAsync(Guid id, Guid profileId, Guid mediaItemId, CancellationToken ct = default)
    {
        var playlist = await playlists.GetWithItemsAsync(id, ct);
        if (playlist is null) return Error.NotFound("Playlist", id);
        if (playlist.ProfileId != profileId) return Error.Forbidden();

        playlist.RemoveItem(mediaItemId);
        await uow.SaveChangesAsync(ct);
        return await ToDetailAsync(playlist, ct);
    }

    public async Task<Result<PlaylistDetailDto>> ReorderItemAsync(Guid id, Guid profileId, ReorderPlaylistItemRequest req, CancellationToken ct = default)
    {
        var playlist = await playlists.GetWithItemsAsync(id, ct);
        if (playlist is null) return Error.NotFound("Playlist", id);
        if (playlist.ProfileId != profileId) return Error.Forbidden();

        playlist.ReorderItem(req.MediaItemId, req.NewOrder);
        await uow.SaveChangesAsync(ct);
        return await ToDetailAsync(playlist, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid profileId, CancellationToken ct = default)
    {
        var playlist = await playlists.GetByIdAsync(id, ct);
        if (playlist is null) return Result.Failure(Error.NotFound("Playlist", id));
        if (playlist.ProfileId != profileId) return Result.Failure(Error.Forbidden());

        playlists.Remove(playlist);
        await uow.SaveChangesAsync(ct);
        log.LogInformation("Profile {ProfileId} deleted playlist {PlaylistId}", profileId, id);
        return Result.Success();
    }

    // ── mapping ──────────────────────────────────────────────────────────────

    private static PlaylistDto ToDto(Playlist p) => new(
        Id: p.Id,
        ProfileId: p.ProfileId,
        Name: p.Name,
        Description: p.Description,
        IsPublic: p.IsPublic,
        CoverImagePath: p.CoverImagePath,
        ItemCount: p.Items.Count,
        CreatedAt: p.CreatedAt,
        UpdatedAt: p.UpdatedAt);

    private async Task<PlaylistDetailDto> ToDetailAsync(Playlist p, CancellationToken ct)
    {
        var mediaIds = p.Items.Select(i => i.MediaItemId).ToList();
        var mediaMap = mediaIds.Count == 0
            ? new Dictionary<Guid, Domain.Media.MediaItem>()
            : (await media.ByIdsAsync(mediaIds, ct)).ToDictionary(m => m.Id);

        var items = p.Items
            .OrderBy(i => i.Order)
            .Select(i =>
            {
                mediaMap.TryGetValue(i.MediaItemId, out var m);
                return new PlaylistItemDto(
                    Id: i.Id,
                    MediaItemId: i.MediaItemId,
                    Title: m?.Title ?? "Unknown",
                    Kind: m?.Kind ?? Domain.Media.MediaKind.Video,
                    PosterPath: m?.PosterPath,
                    Rating: m?.Rating,
                    Year: m?.ReleaseDate?.Year,
                    Order: i.Order,
                    AddedAt: i.AddedAt);
            })
            .ToList();

        return new PlaylistDetailDto(
            Id: p.Id,
            ProfileId: p.ProfileId,
            Name: p.Name,
            Description: p.Description,
            IsPublic: p.IsPublic,
            CoverImagePath: p.CoverImagePath,
            Items: items,
            CreatedAt: p.CreatedAt,
            UpdatedAt: p.UpdatedAt);
    }
}
