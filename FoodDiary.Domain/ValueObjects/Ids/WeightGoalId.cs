using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WeightGoalId(Guid Value) : IEntityId<Guid> {
    public static WeightGoalId New() => new(Guid.NewGuid());
    public static WeightGoalId Empty => new(Guid.Empty);
    public static implicit operator Guid(WeightGoalId id) => id.Value;
    public static explicit operator WeightGoalId(Guid value) => new(value);
}
