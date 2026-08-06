using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WaistGoalId(Guid Value) : IEntityId<Guid> {
    public static WaistGoalId New() => new(Guid.NewGuid());
    public static WaistGoalId Empty => new(Guid.Empty);
    public static implicit operator Guid(WaistGoalId id) => id.Value;
    public static explicit operator WaistGoalId(Guid value) => new(value);
}
