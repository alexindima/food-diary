using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FavoriteRecipeId(Guid Value) : IEntityId<Guid> {
    public static FavoriteRecipeId New() => new(Guid.NewGuid());
    public static FavoriteRecipeId Empty => new(Guid.Empty);

    public static implicit operator Guid(FavoriteRecipeId id) => id.Value;
    public static explicit operator FavoriteRecipeId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
