using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Billing.Presentation.Requests;

public sealed record TelegramStarsUpdateHttpRequest(
    [Range(0, long.MaxValue)] long UpdateId,
    [Range(1, long.MaxValue)] long TelegramUserId,
    [Required, MaxLength(32)] string Kind,
    [Required, MaxLength(128)] string InvoicePayload,
    [MaxLength(3)] string? Currency = null,
    int? TotalAmount = null,
    [MaxLength(255)] string? PaymentChargeId = null,
    DateTime? SubscriptionExpirationUtc = null,
    DateTime? PaymentOccurredAtUtc = null,
    bool IsRecurring = false,
    bool IsFirstRecurring = false,
    [MaxLength(32)] string? SubscriptionState = null);
