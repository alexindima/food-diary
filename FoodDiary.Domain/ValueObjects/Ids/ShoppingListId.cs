using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ShoppingListId(Guid Value) : IEntityId<Guid> {
    public static ShoppingListId New() => new(Guid.NewGuid());
    public static ShoppingListId Empty => new(Guid.Empty);

    public static implicit operator Guid(ShoppingListId id) => id.Value;
    public static explicit operator ShoppingListId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
