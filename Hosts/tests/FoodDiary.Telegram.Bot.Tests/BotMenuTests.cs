using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotMenuTests {
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
