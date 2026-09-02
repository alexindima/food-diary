namespace FoodDiary.Domain.Primitives.Tests;

[ExcludeFromCodeCoverage]
public sealed class VisibilityContractTests {
    [Fact]
    public void Visibility_PreservesPublicContractNamesAndNumbers() {
        Assert.Equal(["Public", "Private"], Enum.GetNames<Visibility>());
        Assert.Equal([0, 1], Enum.GetValues<Visibility>().Select(value => (int)value));
    }
}
