namespace FoodDiary.Telegram.Bot.Tests;

public sealed class TelegramBotOptionsTests {
    [Fact]
    public void HasValidWebAppUrl_WithRelativeUrl_ReturnsFalse() {
        Assert.False(TelegramBotOptions.HasValidWebAppUrl(new TelegramBotOptions {
            WebAppUrl = "/relative",
        }));
    }

    [Fact]
    public void HasValidApiBaseUrl_WithAbsoluteUrl_ReturnsTrue() {
        Assert.True(TelegramBotOptions.HasValidApiBaseUrl(new TelegramBotOptions {
            ApiBaseUrl = "https://api.example.com",
        }));
    }

    [Fact]
    public void HasValidApiSecret_WithShortSecret_ReturnsFalse() {
        Assert.False(TelegramBotOptions.HasValidApiSecret(new TelegramBotOptions {
            ApiSecret = "short-secret",
        }));
    }

    [Fact]
    public void HasValidApiSecret_WithEmptySecret_ReturnsTrue() {
        Assert.True(TelegramBotOptions.HasValidApiSecret(new TelegramBotOptions()));
    }
}
