using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record CycleDayUpsertedDomainEvent : IDomainEvent {
    public CycleDayUpsertedDomainEvent(CycleId cycleId, DateTime date, bool isCreated, DateTime? occurredOnUtcOverride = null) {
        CycleId = cycleId;
        Date = date;
        IsCreated = isCreated;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public CycleId CycleId { get; }
    public DateTime Date { get; }
    public bool IsCreated { get; }
    public DateTime OccurredOnUtc { get; }
}
