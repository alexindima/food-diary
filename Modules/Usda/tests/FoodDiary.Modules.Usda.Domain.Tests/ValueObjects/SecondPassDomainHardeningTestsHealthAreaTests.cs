using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class SecondPassDomainHardeningTestsHealthAreaTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void HealthAreaScore_RejectsScoreOutsidePercentageRange(int score) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HealthAreaScore(score, HealthAreaGrade.Unknown));
    }
}
