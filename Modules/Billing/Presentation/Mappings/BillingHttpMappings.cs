using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.CreatePortalSession;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Presentation.Requests;

namespace FoodDiary.Modules.Billing.Presentation.Mappings;

public static class BillingHttpMappings {
    extension(CreateCheckoutSessionHttpRequest request) {
        public CreateCheckoutSessionCommand ToCommand(Guid userId, string idempotencyKey) =>
                new(userId, request.Plan, request.Provider, idempotencyKey);
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
