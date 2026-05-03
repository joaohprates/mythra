using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Persistence;

namespace Mythra.Api.Controllers;

public sealed record UpdateLanguageRequest(
    string? PreferredContentLanguage,
    string? PreferredSubtitleLanguage,
    string? PreferredAudioLanguage,
    bool? ShowOriginalTitle);

public sealed record ProfileLanguageDto(
    Guid ProfileId,
    string? PreferredContentLanguage,
    string? PreferredSubtitleLanguage,
    string? PreferredAudioLanguage,
    bool ShowOriginalTitle);

public sealed record UpdatePreferencesRequest(bool? ShowAdultContent);

public sealed record ProfilePreferencesDto(Guid ProfileId, bool ShowAdultContent);

[ApiController]
[Route("api/v1/profiles")]
[Authorize]
public sealed class ProfilesController(
    IProfileRepository profiles,
    IUnitOfWork uow,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPatch("{profileId:guid}/language")]
    public async Task<IActionResult> UpdateLanguage(
        Guid profileId, [FromBody] UpdateLanguageRequest req, CancellationToken ct)
    {
        var profile = await profiles.GetByIdAsync(profileId, ct);
        if (profile is null) return NotFound(new { error = "NotFound", message = "Profile not found." });
        if (profile.UserId != currentUser.UserId) return Forbid();

        profile.UpdateLanguagePreferences(
            req.PreferredContentLanguage,
            req.PreferredSubtitleLanguage,
            req.PreferredAudioLanguage,
            req.ShowOriginalTitle);

        await uow.SaveChangesAsync(ct);

        return Ok(new ProfileLanguageDto(
            profile.Id,
            profile.PreferredContentLanguage,
            profile.PreferredSubtitleLanguage,
            profile.PreferredAudioLanguage,
            profile.ShowOriginalTitle));
    }

    [HttpGet("{profileId:guid}/language")]
    public async Task<IActionResult> GetLanguage(Guid profileId, CancellationToken ct)
    {
        var profile = await profiles.GetByIdAsync(profileId, ct);
        if (profile is null) return NotFound(new { error = "NotFound", message = "Profile not found." });
        if (profile.UserId != currentUser.UserId) return Forbid();

        return Ok(new ProfileLanguageDto(
            profile.Id,
            profile.PreferredContentLanguage,
            profile.PreferredSubtitleLanguage,
            profile.PreferredAudioLanguage,
            profile.ShowOriginalTitle));
    }

    [HttpGet("{profileId:guid}/preferences")]
    public async Task<IActionResult> GetPreferences(Guid profileId, CancellationToken ct)
    {
        var profile = await profiles.GetByIdAsync(profileId, ct);
        if (profile is null) return NotFound(new { error = "NotFound", message = "Profile not found." });
        if (profile.UserId != currentUser.UserId) return Forbid();
        return Ok(new ProfilePreferencesDto(profile.Id, profile.ShowAdultContent));
    }

    [HttpPatch("{profileId:guid}/preferences")]
    public async Task<IActionResult> UpdatePreferences(
        Guid profileId, [FromBody] UpdatePreferencesRequest req, CancellationToken ct)
    {
        var profile = await profiles.GetByIdAsync(profileId, ct);
        if (profile is null) return NotFound(new { error = "NotFound", message = "Profile not found." });
        if (profile.UserId != currentUser.UserId) return Forbid();

        profile.UpdatePreferences(req.ShowAdultContent);
        await uow.SaveChangesAsync(ct);
        return Ok(new ProfilePreferencesDto(profile.Id, profile.ShowAdultContent));
    }
}
