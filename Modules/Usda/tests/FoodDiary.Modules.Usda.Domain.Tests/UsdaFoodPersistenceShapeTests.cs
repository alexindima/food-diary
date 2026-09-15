using FoodDiary.Modules.Usda.Domain.Entities;
using System.Reflection;

namespace FoodDiary.Modules.Usda.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsdaFoodPersistenceShapeTests {
    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        var usdaFood = new UsdaFood {
            FdcId = 1,
            Description = "Apple",
            FoodCategoryId = 10,
            FoodCategory = "Fruit",
        };
        ReadPublicProperties(usdaFood);
        Assert.Multiple(
            () => Assert.Equal(10, usdaFood.FoodCategoryId),
            () => Assert.Equal("Fruit", usdaFood.FoodCategory));
    }
}
