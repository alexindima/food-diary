namespace FoodDiary.Modules.Billing.Application.Models;

public sealed record BillingRenewalRunResult(int Processed, int Renewed, int Failed);
