using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record RecommendationCreatedDomainEvent(
    RecommendationId RecommendationId,
    UserId DietologistUserId,
    UserId ClientUserId,
    DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride ?? DomainTime.UtcNow;
}
