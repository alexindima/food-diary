namespace FoodDiary.Modules.Notifications.Application.Abstractions.Models;

public sealed record WebPushDeliverySubscription(
    Guid Id,
    string Endpoint,
    string P256Dh,
    string Auth,
    string? Locale);
