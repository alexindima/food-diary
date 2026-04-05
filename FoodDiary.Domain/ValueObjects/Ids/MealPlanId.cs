using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealPlanId(Guid Value) : IEntityId<Guid> {
    public static MealPlanId New() => new(Guid.NewGuid());
    public static MealPlanId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealPlanId id) => id.Value;
    public static explicit operator MealPlanId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
