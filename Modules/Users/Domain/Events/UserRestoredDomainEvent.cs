using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Events;

public sealed record UserRestoredDomainEvent : IDomainEvent {
    public UserRestoredDomainEvent(UserId userId, DateTime? occurredOnUtcOverride = null) {
        UserId = userId;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public UserId UserId { get; }
    public DateTime OccurredOnUtc { get; }
}
