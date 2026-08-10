using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WeeklyGoalId(Guid Value) : IEntityId<Guid> {
    public static WeeklyGoalId New() => new(Guid.NewGuid());
    public static WeeklyGoalId Empty => new(Guid.Empty);
    public static implicit operator Guid(WeeklyGoalId id) => id.Value;
    public static explicit operator WeeklyGoalId(Guid value) => new(value);
}
