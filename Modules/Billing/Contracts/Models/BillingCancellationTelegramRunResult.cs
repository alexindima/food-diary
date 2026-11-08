namespace FoodDiary.Modules.Billing.Contracts.Models;

public sealed record BillingCancellationTelegramRunResult(int Sent, int Skipped, int Failed);
