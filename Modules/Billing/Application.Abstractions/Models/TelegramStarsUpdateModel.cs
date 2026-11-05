namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record TelegramStarsUpdateModel(long UpdateId, long TelegramUserId, string Kind, string InvoicePayload,
    string? Currency = null, int? TotalAmount = null, string? PaymentChargeId = null,
    DateTime? SubscriptionExpirationUtc = null, DateTime? PaymentOccurredAtUtc = null,
    bool IsRecurring = false, bool IsFirstRecurring = false, string? SubscriptionState = null);
