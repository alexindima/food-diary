using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecipeCommentId(Guid Value) : IEntityId<Guid> {
    public static RecipeCommentId New() => new(Guid.NewGuid());
    public static RecipeCommentId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeCommentId id) => id.Value;
    public static explicit operator RecipeCommentId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
