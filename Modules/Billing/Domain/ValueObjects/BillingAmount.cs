using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Domain.ValueObjects;

public sealed record BillingAmount {
    public decimal Value { get; }
    private BillingAmount(decimal value) => Value = value;

    public static BillingAmount? FromOptional(decimal? amount, string paramName = "amount") {
        decimal? value = BillingDomainGuard.OptionalNumeric19Scale3(amount, paramName);
        return value.HasValue ? new BillingAmount(value.Value) : null;
    }
}
