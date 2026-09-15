namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetTelegramConfiguration;

public sealed record TelegramConfigurationModel(bool LoginEnabled, bool RegistrationEnabled, bool OidcEnabled);
