namespace FoodDiary.Application.Abstractions.Billing.Models;

public sealed record BillingWebhookEventModel(
    string EventId,
    string EventType,
    string ExternalCustomerId,
    string? ExternalSubscriptionId,
    string? ExternalPriceId,
    string? Plan,
    string Status,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAtUtc,
    DateTime? TrialStartUtc,
    DateTime? TrialEndUtc,
    Guid? UserId);
