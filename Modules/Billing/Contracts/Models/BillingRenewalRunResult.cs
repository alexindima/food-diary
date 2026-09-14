namespace FoodDiary.Modules.Billing.Contracts.Models;

public sealed record BillingRenewalRunResult(int Processed, int Renewed, int Failed);
