using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealItemId(Guid Value) : IEntityId<Guid> {
    public static MealItemId New() => new(Guid.NewGuid());
    public static MealItemId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealItemId id) => id.Value;
    public static explicit operator MealItemId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
