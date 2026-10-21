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

    [Fact]
    public void ScoreSnapshotCopy_ReplacesValidatedValuesWithoutChangingAnyOriginalArea() {
        var original = new HealthAreaScores(
            new HealthAreaScore(10, HealthAreaGrade.Low), new HealthAreaScore(25, HealthAreaGrade.Fair),
            new HealthAreaScore(50, HealthAreaGrade.Good), new HealthAreaScore(75, HealthAreaGrade.Excellent),
            new HealthAreaScore(100, HealthAreaGrade.Excellent));

        HealthAreaScores copied = original with {
            Heart = new HealthAreaScore(20, HealthAreaGrade.Low),
            Bone = new HealthAreaScore(30, HealthAreaGrade.Fair),
            Immune = new HealthAreaScore(55, HealthAreaGrade.Good),
            Energy = new HealthAreaScore(80, HealthAreaGrade.Excellent),
            Antioxidant = new HealthAreaScore(95, HealthAreaGrade.Excellent),
        };

        Assert.Multiple(
            () => Assert.Equal(10, original.Heart.Score),
            () => Assert.Equal(25, original.Bone.Score),
            () => Assert.Equal(50, original.Immune.Score),
            () => Assert.Equal(75, original.Energy.Score),
            () => Assert.Equal(100, original.Antioxidant.Score),
            () => Assert.Equal(20, copied.Heart.Score),
            () => Assert.Equal(30, copied.Bone.Score),
            () => Assert.Equal(55, copied.Immune.Score),
            () => Assert.Equal(80, copied.Energy.Score),
            () => Assert.Equal(95, copied.Antioxidant.Score),
            () => Assert.Equal(new HealthAreaScores(
                new HealthAreaScore(20, HealthAreaGrade.Low), new HealthAreaScore(30, HealthAreaGrade.Fair),
                new HealthAreaScore(55, HealthAreaGrade.Good), new HealthAreaScore(80, HealthAreaGrade.Excellent),
                new HealthAreaScore(95, HealthAreaGrade.Excellent)), copied));
    }
}
