namespace FoodDiary.Application.Abstractions.Notifications.Common;

public sealed record WebPushClientConfiguration(bool Enabled, string? PublicKey);
