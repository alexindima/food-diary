using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Integrations.Options;

public sealed class BillingOptions {
    public const string SectionName = "Billing";

    public string Provider { get; init; } = BillingProviderNames.Stripe;
}
