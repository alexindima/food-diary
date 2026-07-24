namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecommendationTemplateId(Guid Value) {
    public static RecommendationTemplateId New() => new(Guid.NewGuid());
}
