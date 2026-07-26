namespace FoodDiary.Application.Billing.Models;

public sealed record BillingRenewalRunResult(int Processed, int Renewed, int Failed);
