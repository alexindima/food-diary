using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Application.Tests.Consumptions;

public class ConsumptionsFeatureTests {
    [Fact]
    public void ConsumptionItemValidator_WhenIdsAreMissing_Fails() {
        var result = ConsumptionItemValidator.Validate(new ConsumptionItemInput(null, null, 100));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void ManualNutritionValidator_WhenAlcoholIsNull_DefaultsToZero() {
        var result = ManualNutritionValidator.Validate(100, 10, 5, 20, 3, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Alcohol);
    }

    [Fact]
    public void SatietyLevelValidator_WhenPreMealOutOfRange_UsesContractFieldName() {
        var result = SatietyLevelValidator.Validate(-1, 5);

        Assert.True(result.IsFailure);
        Assert.Contains("PreMealSatietyLevel", result.Error.Message);
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
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create("Banana", null, 100, "g", 89, 1.1, 0.3, 23, 2.6, 0)
            ]);

        var result = MealNutritionCalculator.Calculate(
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
    public void ConsumptionHttpMappings_CreateToCommand_WhenListsAreNull_MapsEmptyCollections() {
        var request = new CreateConsumptionHttpRequest(
            DateTime.UtcNow,
            MealType.Breakfast.ToString(),
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            Items: null!,
            AiSessions: null);

        var command = request.ToCommand(Guid.NewGuid());

        Assert.Empty(command.Items);
        Assert.Empty(command.AiSessions);
    }

    [Fact]
    public void ConsumptionHttpMappings_UpdateToCommand_WhenAiItemsAreNull_MapsEmptyCollection() {
        var request = new UpdateConsumptionHttpRequest(
            DateTime.UtcNow,
            MealType.Dinner.ToString(),
            Comment: "ok",
            ImageUrl: null,
            ImageAssetId: null,
            Items: [
                new ConsumptionItemHttpRequest(ProductId.New().Value, null, 150)
            ],
            AiSessions: [
                new ConsumptionAiSessionHttpRequest(
                    ImageAssetId: null,
                    RecognizedAtUtc: DateTime.UtcNow,
                    Notes: null,
                    Items: null!)
            ]);

        var command = request.ToCommand(Guid.NewGuid(), Guid.NewGuid());

        var aiSession = Assert.Single(command.AiSessions);
        Assert.Empty(aiSession.Items);
    }
}
