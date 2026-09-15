namespace FoodDiary.Modules.Notifications.Presentation.Responses;

public sealed record WebPushConfigurationHttpResponse(bool Enabled, string? PublicKey);
