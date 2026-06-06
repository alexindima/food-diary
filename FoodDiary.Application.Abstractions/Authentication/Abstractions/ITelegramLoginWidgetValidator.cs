using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramLoginWidgetValidator {
    Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data);
}
