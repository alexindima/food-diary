using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

internal static class BillingWebhookEventValidator {
    public static Error? Validate(BillingWebhookEventModel webhookEvent) {
        if (string.IsNullOrWhiteSpace(webhookEvent.EventId)) {
            return Errors.Billing.WebhookValidationFailed("Webhook event id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.EventType)) {
            return Errors.Billing.WebhookValidationFailed("Webhook event type is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)) {
            return Errors.Billing.WebhookValidationFailed("Webhook customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.Status)) {
            return Errors.Billing.WebhookValidationFailed("Webhook subscription status is required.");
        }

        return null;
    }
}
