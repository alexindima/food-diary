using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Domain.ValueObjects;

public sealed record BillingCurrencyCode {
    public string Value { get; }
    private BillingCurrencyCode(string value) => Value = value;

    public static BillingCurrencyCode? FromOptional(string? currency, string paramName = "currency") {
        string? value = BillingDomainGuard.OptionalCurrencyCode(currency, paramName);
        return value is null ? null : new BillingCurrencyCode(value);
    }
}
