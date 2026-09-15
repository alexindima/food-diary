using FoodDiary.Modules.Usda.Domain.ValueObjects;

namespace FoodDiary.Modules.Usda.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class HealthAreaScoreBoundaryTests {
    [Fact]
    public void ScoreValueObjects_RejectIncoherentOrInvalidInputs() {
        Assert.Throws<ArgumentException>(() => new HealthAreaScore(80, HealthAreaGrade.Low));
        Assert.Throws<ArgumentOutOfRangeException>(() => HealthAreaScores.Calculate(
            new Dictionary<int, double> { [1092] = double.PositiveInfinity },
            new Dictionary<int, double> { [1092] = 100 }));
    }
}
