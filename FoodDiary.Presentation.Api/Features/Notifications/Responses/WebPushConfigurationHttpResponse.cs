namespace FoodDiary.Presentation.Api.Features.Notifications.Responses;

public sealed record WebPushConfigurationHttpResponse(bool Enabled, string? PublicKey);
