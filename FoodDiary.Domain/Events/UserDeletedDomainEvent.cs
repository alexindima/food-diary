using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record UserDeletedDomainEvent : IDomainEvent {
    public UserDeletedDomainEvent(UserId userId, DateTime deletedAtUtc, DateTime? occurredOnUtcOverride = null) {
        UserId = userId;
        DeletedAtUtc = deletedAtUtc;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public UserId UserId { get; }
    public DateTime DeletedAtUtc { get; }
    public DateTime OccurredOnUtc { get; }
}
