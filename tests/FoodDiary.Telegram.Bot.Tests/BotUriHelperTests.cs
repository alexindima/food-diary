namespace FoodDiary.Telegram.Bot.Tests;

public sealed class BotUriHelperTests {
    [Fact]
    public void TryCreateApiBaseUri_WithValidUrl_ReturnsTrue() {
        var ok = BotUriHelper.TryCreateApiBaseUri("https://api.example.com/", out var uri);

        Assert.True(ok);
        Assert.NotNull(uri);
        Assert.Equal("https://api.example.com/", uri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-url")]
    public void TryCreateApiBaseUri_WithInvalidUrl_ReturnsFalse(string? input) {
        var ok = BotUriHelper.TryCreateApiBaseUri(input, out var uri);

        Assert.False(ok);
        Assert.Null(uri);
    }

    [Fact]
    public void NormalizeWebAppUrl_RemovesTrailingSlash() {
        var normalized = BotUriHelper.NormalizeWebAppUrl("https://app.example.com/");

        Assert.Equal("https://app.example.com", normalized);
    }

    [Fact]
    public void NormalizeWebAppUrl_WithWhitespace_ReturnsNull() {
        var normalized = BotUriHelper.NormalizeWebAppUrl("   ");

        Assert.Null(normalized);
    }
}
