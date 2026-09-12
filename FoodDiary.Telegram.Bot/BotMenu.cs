using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot;

internal static class BotMenu {
    internal static bool IsRussian(string? language) => language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;

    internal static string Help(string? language, bool operationsEnabled) {
        bool ru = IsRussian(language);
        string basic = ru
            ? "Доступные команды:\n/start — меню\n/water — добавить воду\n/settings — настройки\n/help — помощь"
            : "Available commands:\n/start — menu\n/water — add water\n/settings — settings\n/help — help";
        if (!operationsEnabled) {
            return basic;
        }
        return basic + (ru
            ? "\n/today — сегодня\n/week — 7 дней\n\nОтправьте одно фото еды. После успешного распознавания приём пищи сохранится автоматически; в ответе будет кнопка отмены. Подпись к фото поможет уточнить состав. Редактирование доступно в дневнике."
            : "\n/today — today\n/week — 7 days\n\nSend one food photo. After successful recognition, the meal is saved automatically with an Undo button in the reply. Add a caption to describe the food. Edit entries in your diary.");
    }

    internal static InlineKeyboardMarkup Water(string? language) => new(
        InlineKeyboardButton.WithCallbackData(IsRussian(language) ? "+250 мл" : "+250 ml", "water:250"),
        InlineKeyboardButton.WithCallbackData(IsRussian(language) ? "+500 мл" : "+500 ml", "water:500")
    );

    internal static InlineKeyboardMarkup Actions(TelegramBotOptions options, string? language) {
        bool ru = IsRussian(language);
        var rows = new List<IEnumerable<InlineKeyboardButton>>();
        if (options.OperationsEnabled) {
            rows.Add([
                InlineKeyboardButton.WithCallbackData(ru ? "Добавить еду" : "Add food", "menu:food"),
                InlineKeyboardButton.WithCallbackData(ru ? "Сегодня" : "Today", "stats:today"),
                InlineKeyboardButton.WithCallbackData(ru ? "7 дней" : "7 days", "stats:week"),
            ]);
        }
        rows.Add([InlineKeyboardButton.WithCallbackData(ru ? "Вода" : "Water", "menu:water")]);
        string? url = BotUriHelper.NormalizeWebAppUrl(options.WebAppUrl);
        if (url is not null) {
            rows.Add([
                InlineKeyboardButton.WithWebApp(ru ? "Открыть дневник" : "Open diary", url),
                InlineKeyboardButton.WithWebApp(ru ? "Настройки" : "Settings", $"{url}/profile"),
            ]);
        }
        rows.Add([InlineKeyboardButton.WithCallbackData(ru ? "Помощь" : "Help", "menu:help")]);
        return new InlineKeyboardMarkup(rows);
    }

    internal static InlineKeyboardMarkup? Open(TelegramBotOptions options, string? language, string path) {
        string? url = BotUriHelper.NormalizeWebAppUrl(options.WebAppUrl);
        return url is null ? null : new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp(IsRussian(language) ? "Открыть FoodDiary" : "Open FoodDiary", url + path));
    }
}
