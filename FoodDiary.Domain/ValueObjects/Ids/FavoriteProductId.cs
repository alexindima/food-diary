using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FavoriteProductId(Guid Value) : IEntityId<Guid> {
    public static FavoriteProductId New() => new(Guid.NewGuid());
    public static FavoriteProductId Empty => new(Guid.Empty);

    public static implicit operator Guid(FavoriteProductId id) => id.Value;
    public static explicit operator FavoriteProductId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
