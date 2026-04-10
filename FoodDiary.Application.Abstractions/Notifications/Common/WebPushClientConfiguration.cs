namespace FoodDiary.Application.Notifications.Common;

public sealed record WebPushClientConfiguration(bool Enabled, string? PublicKey);
