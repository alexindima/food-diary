namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record TelegramConfigurationHttpResponse(bool LoginEnabled, bool RegistrationEnabled, bool OidcEnabled);
