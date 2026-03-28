using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record CycleDayUpsertedDomainEvent(CycleId CycleId, DateTime Date, bool IsCreated, DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride ?? Common.DomainTime.UtcNow;
}
