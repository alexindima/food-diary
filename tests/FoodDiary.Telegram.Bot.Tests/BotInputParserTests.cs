namespace FoodDiary.Telegram.Bot.Tests;

public sealed class BotInputParserTests {
    [Fact]
    public void TryParseWaterAmount_WithValidPayload_ReturnsTrue() {
        var ok = BotInputParser.TryParseWaterAmount("water:500", out var amount);

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
        var ok = BotInputParser.TryParseWaterAmount(payload, out var amount);

        Assert.False(ok);
        Assert.Equal(0, amount);
    }
}
