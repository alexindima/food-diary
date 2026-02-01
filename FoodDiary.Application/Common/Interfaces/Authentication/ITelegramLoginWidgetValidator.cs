using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Common.Interfaces.Authentication;

public interface ITelegramLoginWidgetValidator
{
    Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data);
}
