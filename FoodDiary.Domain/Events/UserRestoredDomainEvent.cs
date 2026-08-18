using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record UserRestoredDomainEvent : IDomainEvent {
    public UserRestoredDomainEvent(UserId userId, DateTime? occurredOnUtcOverride = null) {
        UserId = userId;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public UserId UserId { get; }
    public DateTime OccurredOnUtc { get; }
}
