using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.ProcessTelegramStarsUpdate;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;
using FoodDiary.Modules.Billing.Application.Commands.SubmitCancellationFeedback;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Application.Commands.CreatePortalSession;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Presentation.Requests;

namespace FoodDiary.Modules.Billing.Presentation.Mappings;

public static class BillingHttpMappings {
    extension(TelegramStarsUpdateHttpRequest request) {
        public ProcessTelegramStarsUpdateCommand ToCommand() => new(
            new TelegramStarsUpdateModel(request.UpdateId, request.TelegramUserId,
                request.Kind, request.InvoicePayload, request.Currency, request.TotalAmount, request.PaymentChargeId,
                request.SubscriptionExpirationUtc, request.PaymentOccurredAtUtc, request.IsRecurring, request.IsFirstRecurring,
                request.SubscriptionState));
    }

    extension(CreateCheckoutSessionHttpRequest request) {
        public CreateCheckoutSessionCommand ToCommand(Guid userId, string idempotencyKey) =>
                new(userId, request.Plan, request.Provider, idempotencyKey);
    }

    extension(Guid userId) {
        public CancelSubscriptionCommand ToCancelSubscriptionCommand() => new(userId);
        public CreatePortalSessionCommand ToPortalSessionCommand() => new(userId);
        public StartPremiumTrialCommand ToStartPremiumTrialCommand() => new(userId);
        public GetBillingOverviewQuery ToBillingOverviewQuery() => new(userId);
    }

    extension(SubmitCancellationFeedbackHttpRequest request) {
        public SubmitCancellationFeedbackCommand ToCommand(Guid userId, Guid cancellationId) =>
            new(userId, new BillingCancellationId(cancellationId), request.ReasonCode, request.Comment);
    }

    extension(string provider) {
        public ProcessBillingWebhookCommand ToWebhookCommand(string payload, string signatureHeader) =>
                new(provider, payload, signatureHeader, QueueOnly: true);
    }
}
