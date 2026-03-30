using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ShoppingListItemId(Guid Value) : IEntityId<Guid> {
    public static ShoppingListItemId New() => new(Guid.NewGuid());
    public static ShoppingListItemId Empty => new(Guid.Empty);

    public static implicit operator Guid(ShoppingListItemId id) => id.Value;
    public static explicit operator ShoppingListItemId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
