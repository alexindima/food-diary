using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Modules.Meals.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeSnapshotCompatibilityTests {
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
    public void MealItem_SourceSnapshotAndRecipeSnapshot_CoverRemainingPaths() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        MealItem source = meal.AddProduct(ProductId.New(), 100);
        MealItem target = meal.AddRecipe(RecipeId.New(), 2);
        MealAiSession aiSession = meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "Apple",
                    nameLocal: null,
                    amount: 100,
                    unit: "g",
                    calories: 52,
                    proteins: 0.3,
                    fats: 0.2,
                    carbs: 14,
                    fiber: 2.4,
                    alcohol: 0),
            ]);
        MealAiItem aiItem = Assert.Single(aiSession.Items);
        RecipeStep step = CreateRecipe().AddStep(1, "Step");
        RecipeIngredient ingredient = step.AddProductIngredient(ProductId.New(), 100);
        Recipe recipe = CreateRecipe();

        source.ApplyProductSnapshot("Apple", imageUrl: null, MeasurementUnit.G, baseAmount: 100,
            caloriesPerBase: 52, proteinsPerBase: 0.3, fatsPerBase: 0.2, carbsPerBase: 14,
            fiberPerBase: 2.4, alcoholPerBase: 0);
        source.ApplySource(MealAiItemId.New(), MealItemOrigin.AiText);
        source.ApplySource(source.SourceAiItemId, MealItemOrigin.AiText);
        target.CopySourceAndSnapshotFrom(source);
        target.ApplyRecipeSnapshot(recipe.Name, recipe.ImageUrl, recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs, recipe.TotalFiber, recipe.TotalAlcohol);
        target.ApplySource(sourceAiItemId: null, MealItemOrigin.Barcode);
        ReadPublicProperties(source);
        ReadPublicProperties(target);
        ReadPublicProperties(aiSession);
        ReadPublicProperties(aiItem);
        ReadPublicProperties(ingredient);

        Assert.Multiple(
            () => Assert.True(source.HasNutritionSnapshot),
            () => Assert.True(target.HasNutritionSnapshot),
            () => Assert.Equal("serving", target.SnapshotUnit),
            () => Assert.Equal(MealItemOrigin.Barcode, target.Origin),
            () => Assert.Null(target.SourceAiItemId));
    }
}
