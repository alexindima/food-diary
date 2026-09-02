using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class ValueObjectsInvariantTestsDesiredTests {
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DesiredWeight_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => DesiredWeightKg.Create(value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DesiredWaist_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => DesiredWaistCm.Create(value));
    }
}
