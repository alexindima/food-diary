using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;

public readonly record struct BillingPaymentId(Guid Value) : IEntityId<Guid> {
    public static BillingPaymentId New() => new(Guid.NewGuid());
    public static BillingPaymentId Empty => new(Guid.Empty);
    public static implicit operator Guid(BillingPaymentId id) => id.Value;
    public static explicit operator BillingPaymentId(Guid value) => new(value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format) => Value.ToString(format);
}
