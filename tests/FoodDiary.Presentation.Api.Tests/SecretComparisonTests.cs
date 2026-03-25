using FoodDiary.Presentation.Api.Security;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class SecretComparisonTests {
    [Fact]
    public void FixedTimeEquals_WithMatchingSecrets_ReturnsTrue() {
        var result = SecretComparison.FixedTimeEquals("top-secret", "top-secret");

        Assert.True(result);
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentSecrets_ReturnsFalse() {
        var result = SecretComparison.FixedTimeEquals("top-secret", "wrong-secret");

        Assert.False(result);
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentLengths_ReturnsFalse() {
        var result = SecretComparison.FixedTimeEquals("short", "much-longer");

        Assert.False(result);
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("value", null)]
    [InlineData("", "value")]
    [InlineData("value", "")]
    public void FixedTimeEquals_WithMissingSecret_ReturnsFalse(string? expected, string? actual) {
        var result = SecretComparison.FixedTimeEquals(expected, actual);

        Assert.False(result);
    }
}
