using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

internal static class BillingWebhookEventValidator {
    public static Error? Validate(string provider, BillingWebhookEventModel webhookEvent) {
        if (string.IsNullOrWhiteSpace(webhookEvent.EventId)) {
            return Errors.Billing.WebhookValidationFailed("Webhook event id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.EventType)) {
            return Errors.Billing.WebhookValidationFailed("Webhook event type is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId) &&
            string.IsNullOrWhiteSpace(webhookEvent.RelatedTransactionId)) {
            return Errors.Billing.WebhookValidationFailed("Webhook customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.Status)) {
            return Errors.Billing.WebhookValidationFailed("Webhook subscription status is required.");
        }

        if (webhookEvent.UpdatesSubscription &&
            string.Equals(provider, "paddle", StringComparison.OrdinalIgnoreCase) &&
            webhookEvent.Quantity is int quantity &&
            quantity != 1) {
            return Errors.Billing.WebhookValidationFailed(
                "Paddle subscription quantity must be exactly 1. Use a dedicated price for access duration instead of quantity.");
        }

        return null;
    }
}
