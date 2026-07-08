using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record RecommendationCreatedDomainEvent : IDomainEvent {
    public RecommendationCreatedDomainEvent(
        RecommendationId recommendationId,
        UserId dietologistUserId,
        UserId clientUserId,
        DateTime? occurredOnUtcOverride = null) {
        RecommendationId = recommendationId;
        DietologistUserId = dietologistUserId;
        ClientUserId = clientUserId;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public RecommendationId RecommendationId { get; }
    public UserId DietologistUserId { get; }
    public UserId ClientUserId { get; }
    public DateTime OccurredOnUtc { get; }
}
