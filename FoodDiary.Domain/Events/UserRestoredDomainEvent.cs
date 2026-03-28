using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record UserRestoredDomainEvent(UserId UserId, DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride ?? Common.DomainTime.UtcNow;
}
