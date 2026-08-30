namespace FoodDiary.Application.Billing.Models;

public sealed record BillingWebhookInboxRunResult(int Processed, int Failed);
