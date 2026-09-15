using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Events;

public sealed record UserDeletedDomainEvent : IDomainEvent {
    public UserDeletedDomainEvent(UserId userId, DateTime deletedAtUtc, DateTime? occurredOnUtcOverride = null) {
        UserId = userId;
        DeletedAtUtc = deletedAtUtc;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public UserId UserId { get; }
    public DateTime DeletedAtUtc { get; }
    public DateTime OccurredOnUtc { get; }
}
