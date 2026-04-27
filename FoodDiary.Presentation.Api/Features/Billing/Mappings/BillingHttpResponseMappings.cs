using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Presentation.Api.Features.Billing.Responses;

namespace FoodDiary.Presentation.Api.Features.Billing.Mappings;

public static class BillingHttpResponseMappings {
    public static BillingOverviewHttpResponse ToHttpResponse(this BillingOverviewModel model) =>
        new(
            model.IsPremium,
            model.SubscriptionStatus,
            model.Plan,
            model.CurrentPeriodEndUtc,
            model.CancelAtPeriodEnd,
            model.ManageBillingAvailable,
            model.Provider,
            model.PaddleClientToken,
            model.AvailableProviders);

    public static CheckoutSessionHttpResponse ToHttpResponse(this BillingCheckoutSessionModel model) =>
        new(model.SessionId, model.Url, model.Plan);

    public static PortalSessionHttpResponse ToHttpResponse(this BillingPortalSessionModel model) =>
        new(model.Url);
}
