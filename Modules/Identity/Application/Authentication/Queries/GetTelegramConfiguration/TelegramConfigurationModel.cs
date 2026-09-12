namespace FoodDiary.Application.Identity.Authentication.Queries.GetTelegramConfiguration;

public sealed record TelegramConfigurationModel(bool LoginEnabled, bool RegistrationEnabled, bool OidcEnabled);
