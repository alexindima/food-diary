using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct MarketingAttributionEventId(Guid Value) : IEntityId<Guid> {
    public static MarketingAttributionEventId New() => new(Guid.NewGuid());
    public static MarketingAttributionEventId Empty => new(Guid.Empty);

    public static implicit operator Guid(MarketingAttributionEventId id) => id.Value;
    public static explicit operator MarketingAttributionEventId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
