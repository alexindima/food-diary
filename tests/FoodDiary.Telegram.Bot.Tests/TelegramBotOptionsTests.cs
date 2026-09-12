namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramBotOptionsTests {
    [Theory]
    [InlineData("", "https://diary.example", false)]
    [InlineData("http://api:8080", "https://diary.example", true)]
    [InlineData("https://api.example", "http://diary.example", false)]
    [InlineData("https://api.example", "https://diary.example?redirect=x", false)]
    [InlineData("https://api.example", "https://user:password@diary.example", false)]
    public void EnabledOperations_RequireUsableEndpoints(string apiUrl, string webUrl, bool expected) {
        Assert.Equal(expected, TelegramBotOptions.HasOperationConfiguration(new TelegramBotOptions {
            OperationsEnabled = true, Token = "123:test", ApiSecret = "test-only-api-secret",
            ApiBaseUrl = apiUrl, WebAppUrl = webUrl,
        }));
    }

    [Fact]
    public void EnabledOperations_RejectMissingCredentials() {
        Assert.False(TelegramBotOptions.HasOperationConfiguration(new TelegramBotOptions {
            OperationsEnabled = true, ApiBaseUrl = "https://api.example", WebAppUrl = "https://diary.example",
        }));
        Assert.True(TelegramBotOptions.HasOperationConfiguration(new TelegramBotOptions()));
    }

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
