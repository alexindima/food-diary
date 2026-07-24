namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecommendationBulkDispatchId(Guid Value) {
    public static RecommendationBulkDispatchId New() => new(Guid.NewGuid());
}
