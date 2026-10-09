using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;

public readonly record struct BillingSubscriptionId(Guid Value) : IEntityId<Guid> {
    public static BillingSubscriptionId New() => new(Guid.NewGuid());
    public static BillingSubscriptionId Empty => new(Guid.Empty);
    public static implicit operator Guid(BillingSubscriptionId id) => id.Value;
    public static explicit operator BillingSubscriptionId(Guid value) => new(value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format) => Value.ToString(format);
}
