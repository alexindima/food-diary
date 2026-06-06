using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record UserRestoredDomainEvent : IDomainEvent {
    public UserRestoredDomainEvent(UserId userId, DateTime? occurredOnUtcOverride = null) {
        UserId = userId;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public UserId UserId { get; }
    public DateTime OccurredOnUtc { get; }
}
