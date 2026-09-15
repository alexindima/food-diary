namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public sealed record WebPushClientConfiguration(bool Enabled, string? PublicKey);
