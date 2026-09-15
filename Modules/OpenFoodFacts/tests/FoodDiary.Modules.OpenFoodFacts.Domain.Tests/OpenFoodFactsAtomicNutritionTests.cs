using FoodDiary.Modules.OpenFoodFacts.Domain.Entities;

namespace FoodDiary.Modules.OpenFoodFacts.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class OpenFoodFactsAtomicNutritionTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    private static OpenFoodFactsProduct CreateOpenFoodFactsProduct() {
        return OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            "Brand",
            "Dairy",
            imageUrl: null,
            caloriesPer100G: 64,
            proteinsPer100G: 3.2,
            fatsPer100G: 3.5,
            carbsPer100G: 4.8,
            fiberPer100G: 0,
            Now);
    }

    [Fact]
    public void OpenFoodFactsProduct_RejectsPersistenceOverflowAndInvalidNutritionAtomically() {
        OpenFoodFactsProduct product = CreateOpenFoodFactsProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            new string('n', 513),
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: 10,
            proteinsPer100G: 1,
            fatsPer100G: 1,
            carbsPer100G: 1,
            fiberPer100G: 1,
            Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            "Changed",
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: double.NaN,
            proteinsPer100G: 1,
            fatsPer100G: 1,
            carbsPer100G: 1,
            fiberPer100G: 1,
            Now));

        Assert.Multiple(
            () => Assert.Equal("Milk", product.Name),
            () => Assert.Equal(64, product.CaloriesPer100G),
            () => Assert.Equal(1, product.SearchHitCount));
    }
}
