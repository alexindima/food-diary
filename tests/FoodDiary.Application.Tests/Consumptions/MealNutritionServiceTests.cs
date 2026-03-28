using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Consumptions;

public class MealNutritionServiceTests {
    [Fact]
    public async Task CalculateAsync_WhenMealHasNoItems_ReturnsZeroNutrition() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);
        var service = CreateService();

        var result = await service.CalculateAsync(meal, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Calories);
        Assert.Equal(0, result.Value.Proteins);
    }

    [Fact]
    public async Task CalculateAsync_WhenProductNotAccessible_ReturnsFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);
        var missingProductId = ProductId.New();
        meal.AddProduct(missingProductId, 100);

        var service = CreateService(
            products: new Dictionary<ProductId, Product>());

        var result = await service.CalculateAsync(meal, userId);

        Assert.True(result.IsFailure);
        Assert.Contains("NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task CalculateAsync_WhenRecipeNotAccessible_ReturnsFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Dinner);
        var missingRecipeId = RecipeId.New();
        meal.AddRecipe(missingRecipeId, 1);

        var service = CreateService(
            recipes: new Dictionary<RecipeId, Recipe>());

        var result = await service.CalculateAsync(meal, userId);

        Assert.True(result.IsFailure);
        Assert.Contains("NotAccessible", result.Error.Code);
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

        var service = CreateService(
            products: new Dictionary<ProductId, Product> { [product.Id] = product },
            recipes: new Dictionary<RecipeId, Recipe> { [recipe.Id] = recipe });

        var result = await service.CalculateAsync(meal, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(410, result.Value.Calories, 1);
        Assert.Equal(10.4, result.Value.Proteins, 1);
    }

    [Fact]
    public async Task CalculateAsync_WhenMealHasOnlyAiItems_ReturnsSuccess() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Snack);
        meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [MealAiItemData.Create("Cookie", null, 50, "g", 250, 3, 12, 34, 1, 0)]);

        var service = CreateService();

        var result = await service.CalculateAsync(meal, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.Value.Calories);
    }

    private static MealNutritionService CreateService(
        IReadOnlyDictionary<ProductId, Product>? products = null,
        IReadOnlyDictionary<RecipeId, Recipe>? recipes = null) {
        return new MealNutritionService(
            new StubProductLookup(products ?? new Dictionary<ProductId, Product>()),
            new StubRecipeLookup(recipes ?? new Dictionary<RecipeId, Recipe>()));
    }

    private sealed class StubProductLookup(IReadOnlyDictionary<ProductId, Product> products) : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(products);
    }

    private sealed class StubRecipeLookup(IReadOnlyDictionary<RecipeId, Recipe> recipes) : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(recipes);
    }
}
