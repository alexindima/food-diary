using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Common.Interfaces.Authentication;

public interface ITelegramAuthValidator
{
    Result<TelegramInitData> ValidateInitData(string initData);
}
