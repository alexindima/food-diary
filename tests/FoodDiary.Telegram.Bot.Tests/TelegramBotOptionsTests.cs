namespace FoodDiary.Telegram.Bot.Tests;

public sealed class TelegramBotOptionsTests {
    [Fact]
    public void HasValidWebAppUrl_WithRelativeUrl_ReturnsFalse() {
        Assert.False(TelegramBotOptions.HasValidWebAppUrl("/relative"));
    }

    [Fact]
    public void HasValidApiBaseUrl_WithAbsoluteUrl_ReturnsTrue() {
        Assert.True(TelegramBotOptions.HasValidApiBaseUrl("https://api.example.com"));
    }

    [Fact]
    public void HasValidApiSecret_WithShortSecret_ReturnsFalse() {
        Assert.False(TelegramBotOptions.HasValidApiSecret("short-secret"));
    }

    [Fact]
    public void HasValidApiSecret_WithEmptySecret_ReturnsTrue() {
        Assert.True(TelegramBotOptions.HasValidApiSecret(string.Empty));
    }
}
