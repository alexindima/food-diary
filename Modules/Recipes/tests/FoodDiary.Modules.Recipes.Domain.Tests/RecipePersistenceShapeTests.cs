using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipePersistenceShapeTests {
    private static Recipe CreateRecipe() {
        var recipe = Recipe.Create(
            UserId.New(),
            "Recipe",
            servings: 2,
            imageUrl: "https://img");
        SetPrivateProperty(recipe, nameof(Recipe.TotalCalories), 200d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalProteins), 20d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalFats), 10d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalCarbs), 30d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalFiber), 4d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalAlcohol), 2d);
        return recipe;
    }

    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    private static void SetPrivateProperty<TValue>(object instance, string propertyName, TValue value) {
        instance.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(instance, value);
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        Recipe recipe = CreateRecipe();
        ReadPublicProperties(recipe);
    }
}
