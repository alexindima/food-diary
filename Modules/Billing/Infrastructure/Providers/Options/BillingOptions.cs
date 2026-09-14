using FoodDiary.Modules.Billing.Domain.Contracts;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Options;

public sealed class BillingOptions {
    public const string SectionName = "Billing";

    public string Provider { get; init; } = BillingProviderNames.Stripe;
    public bool RequireConfiguredProvider { get; init; }
}
