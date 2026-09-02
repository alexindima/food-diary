using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserProfileMeasurementInvariantTests {
    [Theory]
    [InlineData(0d)]
    [InlineData(500.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ProfileWeight_Create_WithInvalidValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProfileWeightKg.Create(value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(300.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ProfileHeight_Create_WithInvalidValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProfileHeightCm.Create(value));
    }
}
