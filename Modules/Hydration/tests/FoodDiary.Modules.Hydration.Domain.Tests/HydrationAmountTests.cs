using FoodDiary.Modules.Hydration.Domain.ValueObjects;

namespace FoodDiary.Modules.Hydration.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationAmountTests {
    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    [InlineData(10000)]
    public void FromMilliliters_AcceptsEntryBoundsWithoutChangingTheQuantity(int value) {
        var amount = HydrationAmount.FromMilliliters(value);

        Assert.Equal(value, amount.Milliliters);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10001)]
    [InlineData(int.MaxValue)]
    public void FromMilliliters_RejectsInvalidEntryAmounts(int value) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => HydrationAmount.FromMilliliters(value));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void EqualQuantities_HaveValueEquality() {
        var first = HydrationAmount.FromMilliliters(250);
        var second = HydrationAmount.FromMilliliters(250);
        var different = HydrationAmount.FromMilliliters(500);

        Assert.Multiple(() => {
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, different);
        });
    }
}
