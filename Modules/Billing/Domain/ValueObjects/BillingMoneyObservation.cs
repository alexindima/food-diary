namespace FoodDiary.Modules.Billing.Domain.ValueObjects;

public sealed record BillingMoneyObservation {
    public BillingAmount? Amount { get; }
    public BillingCurrencyCode? Currency { get; }
    private BillingMoneyObservation(BillingAmount? amount, BillingCurrencyCode? currency) {
        Amount = amount;
        Currency = currency;
    }

    public static BillingMoneyObservation FromFields(decimal? amount, string? currency) =>
        new(BillingAmount.FromOptional(amount), BillingCurrencyCode.FromOptional(currency));
}
