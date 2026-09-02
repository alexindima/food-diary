using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class AdditionalValueObjectsInvariantTestsHealthAreaTests {
    [Fact]
    public void HealthAreaScores_Calculate_WithEmptyDictionaries_ReturnsUnknown() {
        var result = HealthAreaScores.Calculate(
            new Dictionary<int, double>(),
            new Dictionary<int, double>());

        Assert.Multiple(
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Heart.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Bone.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Immune.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Energy.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Antioxidant.Grade));
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithGoodNutrientAmounts_ReturnsHighScores() {
        // Provide 100% of daily values for heart nutrients (Potassium=1092, Magnesium=1090)
        var amounts = new Dictionary<int, double> {
            [1092] = 4700,  // Potassium
            [1090] = 420,   // Magnesium
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.True(result.Heart.Score >= 75);
        Assert.Equal(HealthAreaGrade.Excellent, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithExcessSodium_PenalizesHeart() {
        var amountsLowSodium = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
            [1093] = 1000,  // Sodium under limit
        };
        var amountsHighSodium = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
            [1093] = 5000,  // Sodium way over limit
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
            [1093] = 2300,  // Sodium DV
        };

        var lowSodium = HealthAreaScores.Calculate(amountsLowSodium, dailyValues);
        var highSodium = HealthAreaScores.Calculate(amountsHighSodium, dailyValues);

        Assert.True(lowSodium.Heart.Score > highSodium.Heart.Score);
    }

    [Fact]
    public void HealthAreaScores_Calculate_ScoreIsClampedTo0_100() {
        var amounts = new Dictionary<int, double> {
            [1092] = 10000,
            [1090] = 10000,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 100,
            [1090] = 100,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.InRange(result.Heart.Score, 0, 100);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithPartialDailyValues_SkipsMissingAndInvalidDailyValues() {
        var amounts = new Dictionary<int, double> {
            [1092] = 2350,
            [1090] = 420,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 0,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(50, result.Heart.Score);
        Assert.Equal(HealthAreaGrade.Good, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithLowAmount_ReturnsLowGrade() {
        var amounts = new Dictionary<int, double> {
            [1092] = 100,
            [1090] = 10,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(HealthAreaGrade.Low, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithFairAmount_ReturnsFairGrade() {
        var amounts = new Dictionary<int, double> {
            [1092] = 1410,
            [1090] = 126,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(HealthAreaGrade.Fair, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithZeroAmountAndKnownDailyValues_ReturnsUnknownGrade() {
        var amounts = new Dictionary<int, double>();
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(0, result.Heart.Score);
        Assert.Equal(HealthAreaGrade.Unknown, result.Heart.Grade);
    }
}
