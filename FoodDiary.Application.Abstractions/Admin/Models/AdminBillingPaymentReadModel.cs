namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBillingPaymentReadModel(
    Guid Id,
    Guid UserId,
    string UserEmail,
    Guid? BillingSubscriptionId,
    string Provider,
    string ExternalPaymentId,
    string? ExternalCustomerId,
    string? ExternalSubscriptionId,
    string? ExternalPaymentMethodId,
    string? ExternalPriceId,
    string? Plan,
    string Status,
    string Kind,
    decimal? Amount,
    string? Currency,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    string? WebhookEventId,
    string? ProviderMetadataJson,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
