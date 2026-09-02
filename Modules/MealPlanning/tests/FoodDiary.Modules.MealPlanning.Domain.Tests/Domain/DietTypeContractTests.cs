using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DietTypeContractTests {
    [Fact]
    public void DietType_PreservesNamesAndNumericValues() {
        Assert.Equal(["Balanced", "HighProtein", "LowCarb", "Keto", "Mediterranean", "Vegan", "Vegetarian"], Enum.GetNames<DietType>());
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], Enum.GetValues<DietType>().Select(value => (int)value));
    }
}
