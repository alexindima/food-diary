namespace FoodDiary.Domain.Entities.Billing;

public static class BillingPaymentKinds {
    public const string Checkout = "checkout";
    public const string Renewal = "renewal";
    public const string Webhook = "webhook";
    public const string Transaction = "transaction";
    public const string Adjustment = "adjustment";
    public const string Refund = "refund";
    public const string Credit = "credit";
    public const string Chargeback = "chargeback";
    public const string ChargebackReverse = "chargeback_reverse";
    public const string CreditReverse = "credit_reverse";
}
