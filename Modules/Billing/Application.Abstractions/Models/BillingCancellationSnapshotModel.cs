namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCancellationSnapshotModel(
    string Status,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAtUtc,
    DateTime EffectiveAtUtc,
    DateTime ObservedAtUtc);
