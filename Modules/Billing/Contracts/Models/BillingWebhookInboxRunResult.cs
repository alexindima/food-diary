namespace FoodDiary.Modules.Billing.Contracts.Models;

public sealed record BillingWebhookInboxRunResult(int Processed, int Failed);
