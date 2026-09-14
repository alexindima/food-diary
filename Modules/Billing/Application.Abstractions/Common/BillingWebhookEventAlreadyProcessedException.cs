namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public sealed class BillingWebhookEventAlreadyProcessedException(string provider, string eventId)
    : Exception($"Billing webhook event '{eventId}' for provider '{provider}' has already been processed.") {
    public string Provider { get; } = provider;
    public string EventId { get; } = eventId;
}
