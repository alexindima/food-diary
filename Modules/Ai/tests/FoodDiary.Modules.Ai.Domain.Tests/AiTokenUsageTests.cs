using FoodDiary.Modules.Ai.Domain.ValueObjects;

namespace FoodDiary.Modules.Ai.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class AiTokenUsageTests {
    [Fact]
    public void Counts_AllowZeroAndProviderOverhead() {
        var zero = AiTokenUsage.FromCounts(0, 0, 0);
        var overhead = AiTokenUsage.FromCounts(12, 7, 25);
        Assert.Multiple(
            () => Assert.Equal(0, zero.TotalTokens),
            () => Assert.Equal(12, overhead.InputTokens),
            () => Assert.Equal(7, overhead.OutputTokens),
            () => Assert.Equal(25, overhead.TotalTokens));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(12, 7, 1)]
    [InlineData(int.MaxValue, 1, int.MaxValue)]
    public void ProviderCounts_RejectInvalidGroupsWithoutThrowing(int input, int output, int total) {
        Assert.False(AiTokenUsage.TryFromProviderCounts(input, output, total, out AiTokenUsage? usage));
        Assert.Null(usage);
        Assert.Throws<ArgumentOutOfRangeException>(() => AiTokenUsage.FromCounts(input, output, total));
    }

    [Fact]
    public void DefaultUsage_IsAbsentAndValuesAreImmutable() {
        AiTokenUsage? usage = default;
        Assert.Null(usage);
        Assert.Empty(typeof(AiTokenUsage).GetConstructors());
        Assert.DoesNotContain(typeof(AiTokenUsage).GetProperties(), property => property.SetMethod?.IsPublic == true);
    }
}
