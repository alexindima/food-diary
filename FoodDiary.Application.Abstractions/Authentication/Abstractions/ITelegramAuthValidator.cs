using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramAuthValidator {
    Result<TelegramInitData> ValidateInitData(string initData);
}
