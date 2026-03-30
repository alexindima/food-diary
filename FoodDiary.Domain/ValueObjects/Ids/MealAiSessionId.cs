using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MealAiSessionId(Guid Value) : IEntityId<Guid> {
    public static MealAiSessionId New() => new(Guid.NewGuid());
    public static MealAiSessionId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealAiSessionId id) => id.Value;
    public static explicit operator MealAiSessionId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
