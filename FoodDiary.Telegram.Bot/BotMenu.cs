using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot;

internal static class BotMenu {
    internal const string DashboardPath = "/dashboard";
    internal const string MealPath = "/meals/add";
    internal const string SettingsPath = "/profile";

    private static readonly Shortcut WaterShortcut = new("/water", "💧 Water", "💧 Вода");
    private static readonly Shortcut MealShortcut = new("/meal", "🍽 Add meal", "🍽 Добавить еду");
    private static readonly Shortcut DashboardShortcut = new("/dashboard", "📊 Dashboard", "📊 Дашборд");
    private static readonly Shortcut SettingsShortcut = new("/settings", "⚙️ Settings", "⚙️ Настройки");
    private static readonly Shortcut TodayShortcut = new("/today", "Today", "Сегодня");
    private static readonly Shortcut WeekShortcut = new("/week", "7 days", "7 дней");
    private static readonly Shortcut HelpShortcut = new("/help", "Help", "Помощь");
    private static readonly Shortcut[] Shortcuts = [
        WaterShortcut, MealShortcut, DashboardShortcut, SettingsShortcut, TodayShortcut, WeekShortcut, HelpShortcut,
    ];

    internal static string? ResolveLanguage(string? profileLanguage, string? telegramLanguage) =>
        profileLanguage is "en" or "ru" ? profileLanguage : telegramLanguage;

    internal static bool IsRussian(string? language) => language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;

    internal static string? ResolveShortcut(string? text) {
        foreach (Shortcut shortcut in Shortcuts) {
            if (string.Equals(text, shortcut.English, StringComparison.Ordinal) ||
                string.Equals(text, shortcut.Russian, StringComparison.Ordinal)) {
                return shortcut.Command;
            }
        }
        return null;
    }

    internal static ReplyKeyboardMarkup QuickActions(TelegramBotOptions options, string? language) {
        bool russian = IsRussian(language);
        var rows = new List<IEnumerable<KeyboardButton>>();
        if (BotUriHelper.NormalizeWebAppUrl(options.WebAppUrl) is not null) {
            rows.Add([MealShortcut.Button(russian), WaterShortcut.Button(russian)]);
            rows.Add([DashboardShortcut.Button(russian), SettingsShortcut.Button(russian)]);
        } else {
            rows.Add([WaterShortcut.Button(russian)]);
        }
        if (options.OperationsEnabled) {
            rows.Add([TodayShortcut.Button(russian), WeekShortcut.Button(russian)]);
        }
        rows.Add([HelpShortcut.Button(russian)]);
        return new ReplyKeyboardMarkup(rows) { ResizeKeyboard = true, OneTimeKeyboard = true };
    }

    internal static string Welcome(TelegramBotOptions options, string? language, bool linked) {
        bool russian = IsRussian(language);
        bool hasWebApp = BotUriHelper.NormalizeWebAppUrl(options.WebAppUrl) is not null;
        var sections = new List<string> {
            russian ? "👋 <b>Добро пожаловать в Food Diary!</b>" : "👋 <b>Welcome to Food Diary!</b>",
        };
        if (linked) {
            if (hasWebApp) {
                sections.Add(russian
                    ? "📱 <b>Дневник</b>\nНажмите Food Diary слева от поля сообщения или выберите «Дашборд» в быстрых действиях."
                    : "📱 <b>Diary</b>\nTap Food Diary to the left of the message field or choose Dashboard in quick actions.");
            }
            sections.Add(russian
                ? "⚡ <b>Быстрые действия</b>\nКнопки под чатом. Если они скрыты, нажмите значок клавиатуры рядом с полем сообщения."
                : "⚡ <b>Quick actions</b>\nUse the buttons below the chat. If they’re hidden, tap the keyboard icon next to the message field.");
        } else {
            if (hasWebApp) {
                sections.Add(russian
                    ? "📱 Нажмите «Открыть FoodDiary», чтобы войти или создать аккаунт через Telegram."
                    : "📱 Tap Open FoodDiary to sign in or create an account with Telegram.");
            } else {
                sections.Add(russian ? "📱 Войдите в Food Diary через Telegram." : "📱 Sign in to Food Diary with Telegram.");
            }
            sections.Add(russian
                ? "После входа отправьте /start, чтобы увидеть быстрые действия."
                : "After signing in, send /start to see the quick actions.");
        }
        if (options.OperationsEnabled) {
            sections.Add(russian
                ? "📷 <b>Фото еды</b>\nС Food Diary Premium отправьте фото прямо в чат. После распознавания бот сохранит приём пищи. Подпись к фото поможет уточнить состав."
                : "📷 <b>Food photos</b>\nWith Food Diary Premium, send a photo in this chat. After recognition, the bot saves the meal. Add a caption to clarify what’s in the photo.");
        }
        return string.Join("\n\n", sections);
    }

    internal static string Help(string? language, bool operationsEnabled) {
        bool ru = IsRussian(language);
        string basic = ru
            ? "Доступные команды:\n/start — меню\n/water — добавить воду\n/settings — настройки\n/help — помощь"
            : "Available commands:\n/start — menu\n/water — add water\n/settings — settings\n/help — help";
        if (!operationsEnabled) {
            return basic;
        }
        return basic + (ru
            ? "\n/today — сегодня\n/week — 7 дней\n\nС подпиской Food Diary Premium можно отправить одно фото еды. После успешного распознавания приём пищи сохранится автоматически. В ответе будет кнопка отмены. Подпись к фото поможет уточнить состав. Редактирование доступно в дневнике."
            : "\n/today — today\n/week — 7 days\n\nWith Food Diary Premium, send one food photo. After successful recognition, the meal is saved automatically with an Undo button in the reply. Add a caption to describe the food. Edit entries in your diary.");
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
                InlineKeyboardButton.WithWebApp(ru ? "Дашборд" : "Dashboard", url + DashboardPath),
                InlineKeyboardButton.WithWebApp(ru ? "Создать приём пищи" : "Create meal", url + MealPath),
            ]);
            rows.Add([InlineKeyboardButton.WithWebApp(ru ? "Настройки" : "Settings", url + SettingsPath)]);
        }
        rows.Add([InlineKeyboardButton.WithCallbackData(ru ? "Помощь" : "Help", "menu:help")]);
        return new InlineKeyboardMarkup(rows);
    }

    internal static InlineKeyboardMarkup? Open(TelegramBotOptions options, string? language, string path, string? buttonText = null) {
        string? url = BotUriHelper.NormalizeWebAppUrl(options.WebAppUrl);
        return url is null ? null : new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp(buttonText ?? (IsRussian(language) ? "Открыть FoodDiary" : "Open FoodDiary"), url + path));
    }

    private readonly struct Shortcut(string command, string english, string russian) {
        public string Command { get; } = command;
        public string English { get; } = english;
        public string Russian { get; } = russian;

        public KeyboardButton Button(bool russian) => new(russian ? Russian : English);
    }
}
