using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramAuthValidator {
    Result<TelegramInitData> ValidateInitData(string initData);
}
