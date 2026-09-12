namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface ITelegramIdentityPolicy {
    bool LoginEnabled { get; }
    bool RegistrationEnabled { get; }
}
