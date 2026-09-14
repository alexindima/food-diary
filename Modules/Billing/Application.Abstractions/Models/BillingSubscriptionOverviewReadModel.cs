namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingSubscriptionOverviewReadModel(
    Guid Id,
    Guid UserId,
    string Provider,
    string ExternalCustomerId,
    string? Plan,
    string Status,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    DateTime? NextBillingAttemptUtc);
