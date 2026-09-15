using FoodDiary.Modules.Usda.Domain.ValueObjects;

namespace FoodDiary.Modules.Usda.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class SecondPassDomainHardeningTestsHealthAreaTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void HealthAreaScore_RejectsScoreOutsidePercentageRange(int score) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HealthAreaScore(score, HealthAreaGrade.Unknown));
    }
}
