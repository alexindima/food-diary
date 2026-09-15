using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramLoginWidgetValidator {
    Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data);
}
