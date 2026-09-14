using FoodDiary.Modules.Billing.Application.Abstractions.Common;

namespace FoodDiary.Modules.Billing.Presentation;

public static class BillingWebhookRequestLimits {
    public const int MaximumProviderLength = BillingInputLimits.MaximumProviderLength;
}
