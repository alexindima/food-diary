using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductUsdaEnglishNameTests {
    [Fact]
    public void EnglishName_RequiresAUsdaLinkAndRejectsBlankNamesWithoutChangingTheProduct() {
        var product = Product.Create(UserId.New(), "Гречка", MeasurementUnit.G, baseAmount: 100,
            defaultPortionAmount: null, caloriesPerBase: 343, proteinsPerBase: 13.25, fatsPerBase: 3.4,
            carbsPerBase: 71.5, fiberPerBase: 10, alcoholPerBase: 0);

        InvalidOperationException noLink = Assert.Throws<InvalidOperationException>(() => product.SetUsdaEnglishName("Buckwheat"));
        Assert.Equal("A USDA English name requires a USDA link.", noLink.Message);
        Assert.Null(product.UsdaEnglishName);

        product.LinkToUsdaFood(168462);
        DateTime? modified = product.ModifiedOnUtc;
        Assert.Throws<ArgumentException>("englishName", () => product.SetUsdaEnglishName("   "));
        Assert.Multiple(
            () => Assert.Null(product.UsdaEnglishName),
            () => Assert.Equal(168462, product.UsdaFdcId),
            () => Assert.Equal(modified, product.ModifiedOnUtc));
    }
}
