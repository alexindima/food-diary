using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Application.Billing.Commands.CreatePortalSession;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Presentation.Api.Features.Billing.Requests;

namespace FoodDiary.Presentation.Api.Features.Billing.Mappings;

public static class BillingHttpMappings {
    public static CreateCheckoutSessionCommand ToCommand(this CreateCheckoutSessionHttpRequest request, Guid userId) =>
        new(userId, request.Plan);

    public static CreatePortalSessionCommand ToPortalSessionCommand(this Guid userId) => new(userId);

    public static GetBillingOverviewQuery ToBillingOverviewQuery(this Guid userId) => new(userId);

    public static ProcessBillingWebhookCommand ToWebhookCommand(this string provider, string payload, string signatureHeader) =>
        new(provider, payload, signatureHeader);
}
