using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class AdditionalValueObjectsInvariantTestsDesiredTests {
    [Fact]
    public void DesiredWeightAndWaist_Create_ExposeValues() {
        var weight = DesiredWeightKg.Create(75.5);
        var waist = DesiredWaistCm.Create(82.3);

        Assert.Equal(75.5, weight.Value);
        Assert.Equal(82.3, waist.Value);
    }
}
