using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Application.Billing.Commands.CreatePortalSession;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Commands.StartPremiumTrial;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Presentation.Api.Features.Billing.Requests;

namespace FoodDiary.Presentation.Api.Features.Billing.Mappings;

public static class BillingHttpMappings {
    extension(CreateCheckoutSessionHttpRequest request) {
        public CreateCheckoutSessionCommand ToCommand(Guid userId) =>
                new(userId, request.Plan, request.Provider);
    }

    extension(Guid userId) {
        public CreatePortalSessionCommand ToPortalSessionCommand() => new(userId);
        public StartPremiumTrialCommand ToStartPremiumTrialCommand() => new(userId);
        public GetBillingOverviewQuery ToBillingOverviewQuery() => new(userId);
    }

    extension(string provider) {
        public ProcessBillingWebhookCommand ToWebhookCommand(string payload, string signatureHeader) =>
                new(provider, payload, signatureHeader, QueueOnly: true);
    }
}
