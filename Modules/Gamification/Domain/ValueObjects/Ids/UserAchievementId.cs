using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct UserAchievementId(Guid Value) : IEntityId<Guid> {
    public static UserAchievementId New() => new(Guid.NewGuid());
    public static UserAchievementId Empty => new(Guid.Empty);

    public static implicit operator Guid(UserAchievementId id) => id.Value;
    public static explicit operator UserAchievementId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
