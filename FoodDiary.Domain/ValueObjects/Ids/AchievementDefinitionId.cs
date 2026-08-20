using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct AchievementDefinitionId(Guid Value) : IEntityId<Guid> {
    public static AchievementDefinitionId New() => new(Guid.NewGuid());
    public static AchievementDefinitionId Empty => new(Guid.Empty);

    public static implicit operator Guid(AchievementDefinitionId id) => id.Value;
    public static explicit operator AchievementDefinitionId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
