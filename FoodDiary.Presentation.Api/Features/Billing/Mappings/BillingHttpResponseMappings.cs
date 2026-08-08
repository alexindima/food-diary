using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Presentation.Api.Features.Billing.Responses;

namespace FoodDiary.Presentation.Api.Features.Billing.Mappings;

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
