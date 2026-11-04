using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotMenuTests {
    [Theory]
    [InlineData("ru", "🍽 Добавить еду|💧 Вода|📊 Дашборд|⚙️ Настройки|Сегодня|7 дней|Помощь")]
    [InlineData("en", "🍽 Add meal|💧 Water|📊 Dashboard|⚙️ Settings|Today|7 days|Help")]
    public void QuickActions_LocalizesLabelsAndRoutesTheTextTelegramSends(string language, string labels) {
        var options = new TelegramBotOptions { WebAppUrl = "https://diary.example.com/", OperationsEnabled = true };
        ReplyKeyboardMarkup keyboard = BotMenu.QuickActions(options, language);
        string[] buttons = [.. keyboard.Keyboard.SelectMany(row => row).Select(button => button.Text)];

        Assert.Multiple(
            () => Assert.Equal(labels.Split('|'), buttons, StringComparer.Ordinal),
            () => Assert.Equal(new[] { "/meal", "/water", "/dashboard", "/settings", "/today", "/week", "/help" },
                buttons.Select(BotMenu.ResolveShortcut), StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("", "💧 Water|Help")]
    [InlineData("https://diary.example.com/", "🍽 Add meal|💧 Water|📊 Dashboard|⚙️ Settings|Help")]
    public void QuickActions_OmitsUnavailableActions(string webAppUrl, string labels) {
        var options = new TelegramBotOptions { WebAppUrl = webAppUrl, OperationsEnabled = false };
        ReplyKeyboardMarkup keyboard = BotMenu.QuickActions(options, "en");

        Assert.Equal(labels.Split('|'), keyboard.Keyboard.SelectMany(row => row).Select(button => button.Text), StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("ru", "Сегодня", "Настройки", "Помощь")]
    [InlineData("en", "Today", "Settings", "Help")]
    [InlineData("de", "Today", "Settings", "Help")]
    public void Actions_LocalizesLabelsAndKeepsCallbackContracts(string language, string today, string settings, string help) {
        var options = new TelegramBotOptions { WebAppUrl = "https://diary.example.com/", OperationsEnabled = true };
        InlineKeyboardButton[] buttons = [.. BotMenu.Actions(options, language).InlineKeyboard.SelectMany(row => row)];
        Assert.Multiple(
            () => Assert.Equal(today, buttons.Single(button => string.Equals(button.CallbackData, "stats:today", StringComparison.Ordinal)).Text),
            () => Assert.Equal(settings, buttons.Single(button => string.Equals(button.WebApp?.Url, "https://diary.example.com/profile", StringComparison.Ordinal)).Text),
            () => Assert.Contains(buttons, button => string.Equals(button.WebApp?.Url, "https://diary.example.com/dashboard", StringComparison.Ordinal)),
            () => Assert.Contains(buttons, button => string.Equals(button.WebApp?.Url, "https://diary.example.com/meals/add", StringComparison.Ordinal)),
            () => Assert.Equal(help, buttons.Single(button => string.Equals(button.CallbackData, "menu:help", StringComparison.Ordinal)).Text),
            () => Assert.Contains(buttons, button => string.Equals(button.CallbackData, "menu:food", StringComparison.Ordinal)),
            () => Assert.Contains(buttons, button => string.Equals(button.CallbackData, "menu:water", StringComparison.Ordinal)));
    }

    [Fact]
    public void DisabledOperations_DoesNotAdvertisePhotoOrStatistics() {
        var options = new TelegramBotOptions { OperationsEnabled = false };
        InlineKeyboardButton[] buttons = [.. BotMenu.Actions(options, "en").InlineKeyboard.SelectMany(row => row)];
        Assert.DoesNotContain(buttons, button => button.CallbackData is "menu:food" or "stats:today" or "stats:week");
        Assert.DoesNotContain("/today", BotMenu.Help("en", operationsEnabled: false), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ru", "автоматически", "отмены")]
    [InlineData("en", "automatically", "Undo")]
    public void Help_ExplainsAutosaveAndUndo(string language, string autosave, string undo) {
        string text = BotMenu.Help(language, operationsEnabled: true);
        Assert.Contains(autosave, text, StringComparison.Ordinal);
        Assert.Contains(undo, text, StringComparison.Ordinal);
        Assert.Contains("/week", text, StringComparison.Ordinal);
    }
}
