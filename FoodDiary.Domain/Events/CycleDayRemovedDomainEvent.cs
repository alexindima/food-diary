using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record CycleDayRemovedDomainEvent : IDomainEvent {
    public CycleDayRemovedDomainEvent(CycleId cycleId, DateTime date, DateTime? occurredOnUtcOverride = null) {
        CycleId = cycleId;
        Date = date;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public CycleId CycleId { get; }
    public DateTime Date { get; }
    public DateTime OccurredOnUtc { get; }
}
