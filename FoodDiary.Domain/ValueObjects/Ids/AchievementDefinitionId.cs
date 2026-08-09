namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct AchievementDefinitionId(Guid Value) {
    public static AchievementDefinitionId New() => new(Guid.NewGuid());
}
