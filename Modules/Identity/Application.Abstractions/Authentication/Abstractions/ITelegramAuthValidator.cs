using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramAuthValidator {
    Result<TelegramInitData> ValidateInitData(string initData);
}
