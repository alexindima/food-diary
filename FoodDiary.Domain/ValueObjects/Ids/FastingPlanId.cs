using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FastingPlanId(Guid Value) : IEntityId<Guid> {
    public static FastingPlanId New() => new(Guid.NewGuid());
    public static FastingPlanId Empty => new(Guid.Empty);

    public static implicit operator Guid(FastingPlanId id) => id.Value;
    public static explicit operator FastingPlanId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
