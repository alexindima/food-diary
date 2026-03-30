using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ProductId(Guid Value) : IEntityId<Guid> {
    public static ProductId New() => new(Guid.NewGuid());
    public static ProductId Empty => new(Guid.Empty);

    public static implicit operator Guid(ProductId id) => id.Value;
    public static explicit operator ProductId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
