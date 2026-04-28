using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Domain.Events;

public sealed record MediaItemAddedEvent(Guid MediaItemId, MediaKind Kind, Guid LibraryId) : DomainEventBase;

public sealed record MediaItemUpdatedEvent(Guid MediaItemId, MediaKind Kind, string? Reason) : DomainEventBase;

public sealed record MediaItemRemovedEvent(Guid MediaItemId, MediaKind Kind) : DomainEventBase;

public sealed record LibraryScanCompletedEvent(Guid LibraryId, int Added, int Updated, int Removed) : DomainEventBase;

public sealed record PlaybackStartedEvent(Guid SessionId, Guid UserId, Guid MediaItemId) : DomainEventBase;

public sealed record PlaybackEndedEvent(Guid SessionId, Guid UserId, Guid MediaItemId, TimeSpan Position) : DomainEventBase;

public sealed record ProgressUpdatedEvent(Guid ProfileId, Guid MediaItemId, double PercentComplete) : DomainEventBase;
