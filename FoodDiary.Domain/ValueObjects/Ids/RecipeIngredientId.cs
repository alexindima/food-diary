using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecipeIngredientId(Guid Value) : IEntityId<Guid> {
    public static RecipeIngredientId New() => new(Guid.NewGuid());
    public static RecipeIngredientId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeIngredientId id) => id.Value;
    public static explicit operator RecipeIngredientId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
