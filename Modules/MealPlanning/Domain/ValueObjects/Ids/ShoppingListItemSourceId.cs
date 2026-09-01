using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ShoppingListItemSourceId(Guid Value) : IEntityId<Guid> {
    public static ShoppingListItemSourceId New() => new(Guid.NewGuid());
    public static ShoppingListItemSourceId Empty => new(Guid.Empty);

    public static implicit operator Guid(ShoppingListItemSourceId id) => id.Value;
    public static explicit operator ShoppingListItemSourceId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
