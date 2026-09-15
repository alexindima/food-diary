namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface ITelegramIdentityPolicy {
    bool LoginEnabled { get; }
    bool RegistrationEnabled { get; }
}
