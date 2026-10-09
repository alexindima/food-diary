namespace FoodDiary.Modules.Billing.Domain.ValueObjects;

public sealed record BillingPaymentFinancials {
    public BillingMoneyObservation Transaction { get; }
    public BillingAmount? Tax { get; }
    public BillingAmount? Fee { get; }
    public BillingAmount? Earnings { get; }
    public BillingMoneyObservation Payout { get; }

    private BillingPaymentFinancials(BillingMoneyObservation transaction, BillingAmount? tax, BillingAmount? fee,
        BillingAmount? earnings, BillingMoneyObservation payout) {
        Transaction = transaction;
        Tax = tax;
        Fee = fee;
        Earnings = earnings;
        Payout = payout;
    }

    public static BillingPaymentFinancials FromFields(decimal? amount, string? currency, decimal? tax = null,
        decimal? fee = null, decimal? earnings = null, string? payoutCurrency = null, decimal? payoutEarnings = null) {
        var transaction = BillingMoneyObservation.FromFields(amount, currency);
        var taxValue = BillingAmount.FromOptional(tax, nameof(tax));
        var feeValue = BillingAmount.FromOptional(fee, nameof(fee));
        var earningsValue = BillingAmount.FromOptional(earnings, nameof(earnings));
        var payoutCurrencyValue = BillingCurrencyCode.FromOptional(payoutCurrency, nameof(payoutCurrency));
        var payoutAmount = BillingAmount.FromOptional(payoutEarnings, nameof(payoutEarnings));
        return new BillingPaymentFinancials(transaction, taxValue, feeValue, earningsValue,
            BillingMoneyObservation.FromFields(payoutAmount?.Value, payoutCurrencyValue?.Value));
    }
}
