using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ActivityLevelContractTests {
    [Fact]
    public void ActivityLevel_PreservesNamesAndNumericValues() {
        Assert.Equal(["Minimal", "Light", "Moderate", "High", "Extreme"], Enum.GetNames<ActivityLevel>());
        Assert.Equal([0, 1, 2, 3, 4], Enum.GetValues<ActivityLevel>().Select(value => (int)value));
    }
}
