namespace FoodDiary.Modules.Billing.Application.Models;

public sealed record BillingWebhookInboxRunResult(int Processed, int Failed);
