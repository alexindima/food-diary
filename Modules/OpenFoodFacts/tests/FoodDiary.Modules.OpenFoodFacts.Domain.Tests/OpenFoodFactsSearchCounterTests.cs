using FoodDiary.Modules.OpenFoodFacts.Domain.Entities;
using System.Reflection;

namespace FoodDiary.Modules.OpenFoodFacts.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class OpenFoodFactsSearchCounterTests {
    private static void SetProperty<T>(T instance, string propertyName, object value) {
        PropertyInfo property = typeof(T).GetProperty(propertyName) ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
    }

    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void OpenFoodFactsSearchHitCount_SaturatesAtMaximum() {
        var product = OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: null,
            proteinsPer100G: null,
            fatsPer100G: null,
            carbsPer100G: null,
            fiberPer100G: null,
            Now);
        SetProperty(product, nameof(OpenFoodFactsProduct.SearchHitCount), int.MaxValue);

        product.MarkSeen(Now.AddMinutes(1));

        Assert.Equal(int.MaxValue, product.SearchHitCount);
        Assert.Equal(Now.AddMinutes(1), product.LastSeenAtUtc);
    }
}
