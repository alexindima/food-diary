using FoodDiary.Modules.Exercises.Domain.ValueObjects;

namespace FoodDiary.Modules.Exercises.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExerciseValueTests {
    [Fact]
    public void ExerciseValues_PreserveBoundsRoundingAndDayEncoding() {
        Assert.Multiple(
            () => Assert.Equal(1440, ExerciseDuration.FromMinutes(1440).Minutes),
            () => Assert.Equal(0, BurnedEnergy.FromKilocalories(0).Kilocalories),
            () => Assert.Equal(10000, BurnedEnergy.FromKilocalories(10000).Kilocalories),
            () => Assert.Equal(123.4, BurnedEnergy.FromKilocalories(123.45).Kilocalories),
            () => Assert.Equal(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), ExerciseDay.FromDateTimeEncoding(new DateTime(2026, 10, 8, 23, 0, 0)).ToUtcDateTime()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10000.1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Energy_RejectsInvalidObservations(double value) => Assert.Throws<ArgumentOutOfRangeException>(() => BurnedEnergy.FromKilocalories(value));

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void Duration_RejectsInvalidMinutes(int value) => Assert.Throws<ArgumentOutOfRangeException>(() => ExerciseDuration.FromMinutes(value));

}
