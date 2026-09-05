using FoodDiary.Application.Abstractions.Billing.Common;

namespace FoodDiary.Presentation.Api.Features.Billing;

public static class BillingWebhookRequestLimits {
    public const int MaximumProviderLength = BillingInputLimits.MaximumProviderLength;
}
