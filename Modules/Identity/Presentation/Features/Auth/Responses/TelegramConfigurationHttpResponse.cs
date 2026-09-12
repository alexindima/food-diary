namespace FoodDiary.Presentation.Api.Features.Auth.Responses;

public sealed record TelegramConfigurationHttpResponse(bool LoginEnabled, bool RegistrationEnabled, bool OidcEnabled);
