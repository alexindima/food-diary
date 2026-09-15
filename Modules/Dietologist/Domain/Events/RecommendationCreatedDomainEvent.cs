using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Events;

public sealed record RecommendationCreatedDomainEvent : IDomainEvent {
    public RecommendationCreatedDomainEvent(
        RecommendationId recommendationId,
        UserId dietologistUserId,
        UserId clientUserId,
        DateTime? occurredOnUtcOverride = null) {
        RecommendationId = recommendationId;
        DietologistUserId = dietologistUserId;
        ClientUserId = clientUserId;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public RecommendationId RecommendationId { get; }
    public UserId DietologistUserId { get; }
    public UserId ClientUserId { get; }
    public DateTime OccurredOnUtc { get; }
}
