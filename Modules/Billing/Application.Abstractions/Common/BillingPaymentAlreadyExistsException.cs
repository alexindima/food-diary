namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public sealed class BillingPaymentAlreadyExistsException(string provider, string externalPaymentId)
    : Exception($"Billing payment '{externalPaymentId}' for provider '{provider}' already exists.") {
    public string Provider { get; } = provider;
    public string ExternalPaymentId { get; } = externalPaymentId;
}
