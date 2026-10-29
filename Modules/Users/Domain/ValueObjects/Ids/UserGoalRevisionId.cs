using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

public readonly record struct UserGoalRevisionId(Guid Value) : IEntityId<Guid> {
    public static UserGoalRevisionId New() => new(Guid.NewGuid());
    public static UserGoalRevisionId Empty => new(Guid.Empty);
    public static implicit operator Guid(UserGoalRevisionId id) => id.Value;
    public static explicit operator UserGoalRevisionId(Guid value) => new(value);
}
