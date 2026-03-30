using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealAiItemId(Guid Value) : IEntityId<Guid> {
    public static MealAiItemId New() => new(Guid.NewGuid());
    public static MealAiItemId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealAiItemId id) => id.Value;
    public static explicit operator MealAiItemId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
