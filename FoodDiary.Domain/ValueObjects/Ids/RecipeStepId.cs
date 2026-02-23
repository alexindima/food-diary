using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecipeStepId(Guid Value) : IEntityId<Guid> {
    public static RecipeStepId New() => new(Guid.NewGuid());
    public static RecipeStepId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeStepId id) => id.Value;
    public static explicit operator RecipeStepId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
