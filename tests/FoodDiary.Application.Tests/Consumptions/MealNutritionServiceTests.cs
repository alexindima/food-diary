using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Tests.Consumptions;

[ExcludeFromCodeCoverage]
public class MealNutritionServiceTests {
    [Fact]
    public async Task CalculateAsync_WhenMealHasNoItems_ReturnsZeroNutrition() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);
        MealNutritionService service = CreateService();

        Result<MealNutritionSummary> result = await service.CalculateAsync(meal, userId);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.Calories);
        Assert.Equal(0, result.Value.Proteins);
    }

    [Fact]
    public async Task CalculateAsync_WhenProductNotAccessible_ReturnsFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);
        var missingProductId = ProductId.New();
        meal.AddProduct(missingProductId, 100);

        MealNutritionService service = CreateService(
            products: new Dictionary<ProductId, Product>());

        Result<MealNutritionSummary> result = await service.CalculateAsync(meal, userId);

        ResultAssert.Failure(result);
        Assert.Contains("NotAccessible", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateAsync_WhenRecipeNotAccessible_ReturnsFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Dinner);
        var missingRecipeId = RecipeId.New();
        meal.AddRecipe(missingRecipeId, 1);

        MealNutritionService service = CreateService(
            recipes: new Dictionary<RecipeId, Recipe>());

        Result<MealNutritionSummary> result = await service.CalculateAsync(meal, userId);

        ResultAssert.Failure(result);
        Assert.Contains("NotAccessible", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateAsync_WhenProductsAndRecipesExist_CalculatesCorrectly() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);

        var product = Product.Create(
            userId, name: "Rice", baseUnit: MeasurementUnit.G, baseAmount: 100,
            defaultPortionAmount: 100, caloriesPerBase: 130, proteinsPerBase: 2.7,
            fatsPerBase: 0.3, carbsPerBase: 28, fiberPerBase: 0.4, alcoholPerBase: 0);

        var recipe = Recipe.Create(userId, "Salad", servings: 1);
        recipe.SetManualNutrition(150, 5, 10, 8, 3, 0);

        meal.AddProduct(product.Id, 200);
        meal.AddRecipe(recipe.Id, 1);

        MealNutritionService service = CreateService(
            products: new Dictionary<ProductId, Product> { [product.Id] = product },
            recipes: new Dictionary<RecipeId, Recipe> { [recipe.Id] = recipe });

        Result<MealNutritionSummary> result = await service.CalculateAsync(meal, userId);

        ResultAssert.Success(result);
        Assert.Equal(410, result.Value.Calories, 1);
        Assert.Equal(10.4, result.Value.Proteins, 1);
    }

    [Fact]
    public async Task CalculateAsync_WhenMealHasOnlyAiItems_ReturnsSuccess() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Snack);
        meal.AddAiSession(
            imageAssetId: null,
            source: AiRecognitionSource.Text,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [MealAiItemData.Create("Cookie", nameLocal: null, 50, "g", 250, 3, 12, 34, 1, 0)]);

        MealNutritionService service = CreateService();

        Result<MealNutritionSummary> result = await service.CalculateAsync(meal, userId);

        ResultAssert.Success(result);
        Assert.Equal(250, result.Value.Calories);
    }

    [Fact]
    public async Task CalculateAsync_WhenAiItemIsRejected_ExcludesItFromNutrition() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Snack);
        meal.AddAiSession(
            imageAssetId: null,
            source: AiRecognitionSource.Text,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create("Cookie", nameLocal: null, 50, "g", 250, 3, 12, 34, 1, 0),
                MealAiItemData.Create(
                    "Sauce",
                    nameLocal: null,
                    20,
                    "g",
                    100,
                    proteins: 1,
                    fats: 2,
                    carbs: 3,
                    fiber: 0,
                    alcohol: 0,
                    resolution: MealAiItemResolution.Rejected),
            ]);

        MealNutritionService service = CreateService();

        Result<MealNutritionSummary> result = await service.CalculateAsync(meal, userId);

        ResultAssert.Success(result);
        Assert.Equal(250, result.Value.Calories);
    }

    private static MealNutritionService CreateService(
        IReadOnlyDictionary<ProductId, Product>? products = null,
        IReadOnlyDictionary<RecipeId, Recipe>? recipes = null) {
        return new MealNutritionService(
            CreateProductLookup(products ?? new Dictionary<ProductId, Product>()),
            CreateRecipeLookup(recipes ?? new Dictionary<RecipeId, Recipe>()));
    }

    private static IProductLookupService CreateProductLookup(IReadOnlyDictionary<ProductId, Product> products) {
        IProductLookupService lookup = Substitute.For<IProductLookupService>();
        lookup
            .GetAccessibleByIdsAsync(Arg.Any<IEnumerable<ProductId>>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(products));
        return lookup;
    }

    private static IRecipeLookupService CreateRecipeLookup(IReadOnlyDictionary<RecipeId, Recipe> recipes) {
        IRecipeLookupService lookup = Substitute.For<IRecipeLookupService>();
        lookup
            .GetAccessibleByIdsAsync(Arg.Any<IEnumerable<RecipeId>>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipes));
        return lookup;
    }
}
