using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Dietologist;

public sealed class RecommendationBulkDispatch : Entity<RecommendationBulkDispatchId> {
    public UserId DietologistUserId { get; private set; }
    public UserId ClientUserId { get; private set; }
    public RecommendationId RecommendationId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;

    private RecommendationBulkDispatch() {
    }

    public static RecommendationBulkDispatch Create(
        UserId dietologistUserId,
        UserId clientUserId,
        RecommendationId recommendationId,
        string idempotencyKey) {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        string normalizedKey = idempotencyKey.Trim();
        if (normalizedKey.Length > 100) {
            throw new ArgumentOutOfRangeException(nameof(idempotencyKey), normalizedKey.Length, "Idempotency key is too long.");
        }

        var dispatch = new RecommendationBulkDispatch {
            Id = RecommendationBulkDispatchId.New(),
            DietologistUserId = dietologistUserId,
            ClientUserId = clientUserId,
            RecommendationId = recommendationId,
            IdempotencyKey = normalizedKey,
        };
        dispatch.SetCreated();
        return dispatch;
    }
}
