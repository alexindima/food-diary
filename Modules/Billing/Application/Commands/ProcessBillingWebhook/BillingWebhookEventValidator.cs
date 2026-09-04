using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

internal static class BillingWebhookEventValidator {
    public static Error? Validate(string provider, BillingWebhookEventModel webhookEvent) {
        if (string.IsNullOrWhiteSpace(webhookEvent.EventId)) {
            return BillingErrors.WebhookValidationFailed("Webhook event id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.EventType)) {
            return BillingErrors.WebhookValidationFailed("Webhook event type is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId) &&
            string.IsNullOrWhiteSpace(webhookEvent.RelatedTransactionId)) {
            return BillingErrors.WebhookValidationFailed("Webhook customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.Status)) {
            return BillingErrors.WebhookValidationFailed("Webhook subscription status is required.");
        }

        if (webhookEvent.UpdatesSubscription &&
            string.Equals(provider, "paddle", StringComparison.OrdinalIgnoreCase) &&
            webhookEvent.Quantity is { } quantity &&
            quantity != 1) {
            return BillingErrors.WebhookValidationFailed(
                "Paddle subscription quantity must be exactly 1. Use a dedicated price for access duration instead of quantity.");
        }

        return null;
    }
}
