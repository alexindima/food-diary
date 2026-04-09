using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Abstractions;

public interface ITelegramAuthValidator {
    Result<TelegramInitData> ValidateInitData(string initData);
}
