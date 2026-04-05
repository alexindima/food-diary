using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealPlanMealId(Guid Value) : IEntityId<Guid> {
    public static MealPlanMealId New() => new(Guid.NewGuid());
    public static MealPlanMealId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealPlanMealId id) => id.Value;
    public static explicit operator MealPlanMealId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
