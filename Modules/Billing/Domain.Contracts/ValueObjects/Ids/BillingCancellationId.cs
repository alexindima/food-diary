using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;

public readonly record struct BillingCancellationId(Guid Value) : IEntityId<Guid> {
    public static BillingCancellationId New() => new(Guid.NewGuid());
    public static explicit operator BillingCancellationId(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
