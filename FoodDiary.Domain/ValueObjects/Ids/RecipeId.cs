using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecipeId(Guid Value) : IEntityId<Guid> {
    public static RecipeId New() => new(Guid.NewGuid());
    public static RecipeId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeId id) => id.Value;
    public static explicit operator RecipeId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
