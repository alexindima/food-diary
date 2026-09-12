using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotMealActions {
    internal static InlineKeyboardMarkup Create(BotRecognizedMeal meal, string? webAppUrl, bool russian, DateTime nowUtc) {
        var rows = new List<IEnumerable<InlineKeyboardButton>>();
        if (!meal.Undone && meal.UndoUntilUtc > nowUtc) {
            rows.Add([InlineKeyboardButton.WithCallbackData(russian ? "Отменить" : "Undo", $"meal:undo:{meal.OperationId:N}")]);
        }
        string? url = BotUriHelper.NormalizeWebAppUrl(webAppUrl);
        if (url is not null) {
            string label = russian ? "Редактировать" : "Edit";
            if (meal.Undone) {
                label = russian ? "Открыть дневник" : "Open diary";
            }
            rows.Add([InlineKeyboardButton.WithWebApp(
                label,
                meal.Undone ? $"{url}/meals" : $"{url}/meals/{meal.MealId:D}/edit")]);
        }
        rows.Add([InlineKeyboardButton.WithCallbackData(russian ? "Сегодня" : "Today", "stats:today")]);
        return new InlineKeyboardMarkup(rows);
    }
}
