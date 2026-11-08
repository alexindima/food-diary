namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCancellationTelegramSnapshotModel(
    string Provider,
    string ExternalSubscriptionId,
    string Outcome,
    DateTime EffectiveAtUtc,
    DateTime? HandledAtUtc);
