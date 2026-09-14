using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Presentation.Responses;

namespace FoodDiary.Modules.Billing.Presentation.Mappings;

public static class BillingHttpResponseMappings {
    extension(BillingOverviewModel model) {
        public BillingOverviewHttpResponse ToHttpResponse() =>
                new(
                    model.IsPremium,
                    model.SubscriptionStatus,
                    model.Plan,
                    model.SubscriptionProvider,
                    model.CurrentPeriodStartUtc,
                    model.CurrentPeriodEndUtc,
                    model.NextBillingAttemptUtc,
                    model.CancelAtPeriodEnd,
                    model.RenewalEnabled,
                    model.ManageBillingAvailable,
                    model.PremiumTrialStartUtc,
                    model.PremiumTrialEndUtc,
                    model.PremiumTrialActive,
                    model.PremiumTrialUsed,
                    model.CanStartPremiumTrial,
                    model.Provider,
                    model.PaddleClientToken,
                    model.AvailableProviders);
    }

    extension(BillingCheckoutSessionModel model) {
        public CheckoutSessionHttpResponse ToHttpResponse() =>
                new(model.SessionId, model.Url, model.Plan);
    }

    extension(BillingPortalSessionModel model) {
        public PortalSessionHttpResponse ToHttpResponse() =>
                new(model.Url);
    }
}
