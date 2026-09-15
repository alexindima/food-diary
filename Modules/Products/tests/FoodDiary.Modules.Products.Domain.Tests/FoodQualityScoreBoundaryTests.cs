using FoodDiary.Modules.Products.FoodQuality.ValueObjects;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FoodQualityScoreBoundaryTests {
    [Fact]
    public void ScoreValueObjects_RejectIncoherentOrInvalidInputs() {
        Assert.Throws<ArgumentException>(() => new FoodQualityScore(80, FoodQualityGrade.Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FoodQualityScore(101, FoodQualityGrade.Green));
        Assert.Throws<ArgumentOutOfRangeException>(() => FoodQualityScore.Calculate(
            double.NaN, 0, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FoodQualityScore.Calculate(
            100, -1, 0, 0, 0, 0));
    }
}
