using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotMealActions {
    internal static InlineKeyboardMarkup Create(BotRecognizedMeal meal, string? webAppUrl, bool russian, DateTime nowUtc) {
        var actions = new List<InlineKeyboardButton>();
        string? url = BotUriHelper.NormalizeWebAppUrl(webAppUrl);
        if (url is not null) {
            string label = russian ? "Редактировать" : "Edit";
            if (meal.Undone) {
                label = russian ? "Открыть дневник" : "Open diary";
            }
            actions.Add(InlineKeyboardButton.WithWebApp(
                label,
                meal.Undone ? $"{url}/meals" : $"{url}/meals/{meal.MealId.Value:D}/edit"));
        }
        if (!meal.Undone && meal.UndoUntilUtc > nowUtc) {
            actions.Add(InlineKeyboardButton.WithCallbackData(russian ? "Отменить" : "Undo", $"meal:undo:{meal.OperationId.Value:N}"));
        }
        InlineKeyboardButton[] summary = [InlineKeyboardButton.WithCallbackData(russian ? "Сводка дня" : "Daily summary", "stats:today")];
        return new InlineKeyboardMarkup(actions.Count > 0 ? [actions, summary] : [summary]);
    }
}
