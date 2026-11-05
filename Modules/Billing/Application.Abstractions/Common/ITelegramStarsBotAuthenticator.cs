namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface ITelegramStarsBotAuthenticator {
    bool IsAuthenticated(string providedSecret);
}
