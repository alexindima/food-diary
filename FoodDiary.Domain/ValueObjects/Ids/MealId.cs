using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealId(Guid Value) : IEntityId<Guid> {
    public static MealId New() => new(Guid.NewGuid());
    public static MealId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealId id) => id.Value;
    public static explicit operator MealId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
