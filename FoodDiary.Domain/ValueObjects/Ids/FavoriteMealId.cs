using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FavoriteMealId(Guid Value) : IEntityId<Guid> {
    public static FavoriteMealId New() => new(Guid.NewGuid());
    public static FavoriteMealId Empty => new(Guid.Empty);

    public static implicit operator Guid(FavoriteMealId id) => id.Value;
    public static explicit operator FavoriteMealId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
