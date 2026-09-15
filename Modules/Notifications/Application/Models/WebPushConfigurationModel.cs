namespace FoodDiary.Modules.Notifications.Application.Models;

public sealed record WebPushConfigurationModel(bool Enabled, string? PublicKey);
