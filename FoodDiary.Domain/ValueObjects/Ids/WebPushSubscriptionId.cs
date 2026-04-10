using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WebPushSubscriptionId(Guid Value) : IEntityId<Guid> {
    public static WebPushSubscriptionId New() => new(Guid.NewGuid());
    public static WebPushSubscriptionId Empty => new(Guid.Empty);

    public static implicit operator Guid(WebPushSubscriptionId id) => id.Value;
    public static explicit operator WebPushSubscriptionId(Guid value) => new(value);
}
