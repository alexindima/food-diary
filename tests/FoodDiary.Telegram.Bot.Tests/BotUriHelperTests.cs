
namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotUriHelperTests {
    [Fact]
    public void TryCreateApiBaseUri_WithValidUrl_ReturnsTrue() {
        bool ok = BotUriHelper.TryCreateApiBaseUri("https://api.example.com/", out Uri? uri);

        Assert.True(ok);
        Assert.NotNull(uri);
        Assert.Equal("https://api.example.com/", uri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-url")]
    public void TryCreateApiBaseUri_WithInvalidUrl_ReturnsFalse(string? input) {
        bool ok = BotUriHelper.TryCreateApiBaseUri(input, out Uri? uri);

        Assert.False(ok);
        Assert.Null(uri);
    }

    [Fact]
    public void NormalizeWebAppUrl_RemovesTrailingSlash() {
        string? normalized = BotUriHelper.NormalizeWebAppUrl("https://app.example.com/");

        Assert.Equal("https://app.example.com", normalized);
    }

    [Fact]
    public void NormalizeWebAppUrl_WithWhitespace_ReturnsNull() {
        string? normalized = BotUriHelper.NormalizeWebAppUrl("   ");

        Assert.Null(normalized);
    }
}
