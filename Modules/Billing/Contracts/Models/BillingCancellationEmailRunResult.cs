namespace FoodDiary.Modules.Billing.Contracts.Models;

public sealed record BillingCancellationEmailRunResult(int Queued, int Skipped, int Failed);
