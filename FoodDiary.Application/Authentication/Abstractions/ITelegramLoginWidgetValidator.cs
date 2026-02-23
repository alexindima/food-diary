using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Abstractions;

public interface ITelegramLoginWidgetValidator {
    Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data);
}
