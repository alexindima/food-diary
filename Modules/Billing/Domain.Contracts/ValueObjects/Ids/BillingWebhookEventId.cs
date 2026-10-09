using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;

public readonly record struct BillingWebhookEventId(Guid Value) : IEntityId<Guid> {
    public static BillingWebhookEventId New() => new(Guid.NewGuid());
    public static BillingWebhookEventId Empty => new(Guid.Empty);
    public static implicit operator Guid(BillingWebhookEventId id) => id.Value;
    public static explicit operator BillingWebhookEventId(Guid value) => new(value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format) => Value.ToString(format);
}
