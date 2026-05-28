namespace FoodDiary.Application.Notifications.Models;

public sealed record WebPushConfigurationModel(bool Enabled, string? PublicKey);
