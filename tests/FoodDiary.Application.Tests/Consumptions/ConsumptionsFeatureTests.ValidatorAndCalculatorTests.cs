using FoodDiary.Results;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Consumptions;

public partial class ConsumptionsFeatureTests {

    [Fact]
    public void ConsumptionItemValidator_WhenIdsAreMissing_Fails() {
        Result result = ConsumptionItemValidator.Validate(new ConsumptionItemInput(ProductId: null, RecipeId: null, 100));

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }


    [Fact]
    public void ManualNutritionValidator_WhenAlcoholIsNull_DefaultsToZero() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(100, 10, 5, 20, 3, alcohol: null);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.Alcohol);
    }


    [Fact]
    public void SatietyLevelValidator_WhenPreMealOutOfRange_UsesContractFieldName() {
        Result result = SatietyLevelValidator.Validate(-1, 5);

        ResultAssert.Failure(result);
        Assert.Contains("PreMealSatietyLevel", result.Error.Message, StringComparison.Ordinal);
    }


    [Fact]
    public void MealNutritionCalculator_WhenMealHasProductRecipeAndAiItems_CalculatesCombinedTotals() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);

        var product = Product.Create(
            userId,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.SetManualNutrition(200, 10, 4, 20, 2, 0);

        meal.AddProduct(product.Id, 50);
        meal.AddRecipe(recipe.Id, 1);
        meal.AddAiSession(
            imageAssetId: null,
            source: AiRecognitionSource.Text,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create("Banana", nameLocal: null, 100, "g", 89, 1.1, 0.3, 23, 2.6, 0),
            ]);

        MealNutritionSummary result = MealNutritionCalculator.Calculate(
            meal,
            new Dictionary<ProductId, Product> { [product.Id] = product },
            new Dictionary<RecipeId, Recipe> { [recipe.Id] = recipe });

        Assert.Equal(215, result.Calories, 2);
        Assert.Equal(6.25, result.Proteins, 2);
        Assert.Equal(2.4, result.Fats, 2);
        Assert.Equal(40, result.Carbs, 2);
        Assert.Equal(4.8, result.Fiber, 2);
        Assert.Equal(0, result.Alcohol, 2);
    }


    [Fact]
    public void MealNutritionCalculator_WhenRecipeItemIsMissingFromLookup_IgnoresRecipeItem() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);

        meal.AddRecipe(RecipeId.New(), 1);

        MealNutritionSummary result = MealNutritionCalculator.Calculate(
            meal,
            new Dictionary<ProductId, Product>(),
            new Dictionary<RecipeId, Recipe>());

        Assert.Equal(0, result.Calories);
        Assert.Equal(0, result.Proteins);
        Assert.Equal(0, result.Fats);
        Assert.Equal(0, result.Carbs);
        Assert.Equal(0, result.Fiber);
        Assert.Equal(0, result.Alcohol);
    }

}
