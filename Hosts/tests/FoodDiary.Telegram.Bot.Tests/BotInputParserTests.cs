namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotInputParserTests {
    [Fact]
    public void TryParseWaterAmount_WithValidPayload_ReturnsTrue() {
        bool ok = BotInputParser.TryParseWaterAmount("water:500", out int amount);

        Assert.True(ok);
        Assert.Equal(500, amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("water:0")]
    [InlineData("water:-10")]
    [InlineData("water:abc")]
    [InlineData("other:100")]
    public void TryParseWaterAmount_WithInvalidPayload_ReturnsFalse(string? payload) {
        bool ok = BotInputParser.TryParseWaterAmount(payload, out int amount);

        Assert.False(ok);
        Assert.Equal(0, amount);
    }
}
