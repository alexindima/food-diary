using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealPlanDayId(Guid Value) : IEntityId<Guid> {
    public static MealPlanDayId New() => new(Guid.NewGuid());
    public static MealPlanDayId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealPlanDayId id) => id.Value;
    public static explicit operator MealPlanDayId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
