namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminBillingSubscriptionHttpResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string Provider,
    string ExternalCustomerId,
    string? ExternalSubscriptionId,
    string? ExternalPaymentMethodId,
    string? ExternalPriceId,
    string? Plan,
    string Status,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    DateTime? NextBillingAttemptUtc,
    string? LastWebhookEventId,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
