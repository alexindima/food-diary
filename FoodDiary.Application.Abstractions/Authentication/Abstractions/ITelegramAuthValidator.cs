using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramAuthValidator {
    Result<TelegramInitData> ValidateInitData(string initData);
}
