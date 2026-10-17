using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Application.Services;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Tests;

[ExcludeFromCodeCoverage]
public class MealNutritionServiceTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManualNutrition_RejectsInaccessibleProductAndRecipeReferences(bool recipeSource) {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow);
        if (recipeSource) {
            meal.AddRecipe(RecipeId.New(), RecipeServingQuantity.FromServings(1));
        } else {
            meal.AddProduct(ProductId.New(), ProductUnitQuantity.FromUnits(100));
        }
        Result result = await MealNutritionApplier.ApplyAsync(meal, userId, CreateService(),
            new MealNutritionInput(IsNutritionAutoCalculated: false, 600, 30, 20, 50, 5, 0), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Contains("NotAccessible", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManualNutrition_StoresAuthorizedSourceSnapshotWithoutReplacingManualTotals() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Rice", MeasurementUnit.G, 100, 100, 130, 3, 1, 25, 2, 0);
        var meal = Meal.Create(userId, DateTime.UtcNow);
        MealItem item = meal.AddProduct(product.Id, ProductUnitQuantity.FromUnits(200));
        MealNutritionService service = CreateService(products: new Dictionary<ProductId, Product> { [product.Id] = product });
        Result result = await MealNutritionApplier.ApplyAsync(meal, userId, service,
            new MealNutritionInput(IsNutritionAutoCalculated: false, 600, 30, 20, 50, 5, 0), CancellationToken.None);
        ResultAssert.Success(result);
        Assert.False(meal.IsNutritionAutoCalculated);
        Assert.Equal(600, meal.TotalCalories);
        Assert.Equal(30, meal.TotalProteins);
        Assert.True(item.HasNutritionSnapshot);
        Assert.Equal("Rice", item.SnapshotName);
    }

    [Fact]
    public async Task HistoricalSnapshot_RemainsUsableAfterSourceBecomesInaccessible() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow);
        MealItem item = meal.AddProduct(ProductId.New(), ProductUnitQuantity.FromUnits(200));
        item.ApplyProductSnapshot("Historical rice", imageUrl: null, MeasurementUnit.G, 100, 130, 3, 1, 25, 2, 0);
        MealNutritionSummary summary = ResultAssert.Success(await CreateService().CalculateAsync(meal, userId));
        Assert.Equal(260, summary.Calories);
        Assert.Equal("Historical rice", item.SnapshotName);
    }

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
        meal.AddProduct(missingProductId, ProductUnitQuantity.FromUnits(100));

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
        meal.AddRecipe(missingRecipeId, RecipeServingQuantity.FromServings(1));

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

        meal.AddProduct(product.Id, ProductUnitQuantity.FromUnits(200));
        meal.AddRecipe(recipe.Id, RecipeServingQuantity.FromServings(1));

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
            .Returns(call => Task.FromResult<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>>(
                products.ToDictionary(pair => pair.Key, pair => TestProductOverview.From(pair.Value, call.ArgAt<UserId>(1)))));
        return lookup;
    }

    private static IRecipeLookupService CreateRecipeLookup(IReadOnlyDictionary<RecipeId, Recipe> recipes) {
        IRecipeLookupService lookup = Substitute.For<IRecipeLookupService>();
        lookup
            .GetAccessibleByIdsAsync(Arg.Any<IEnumerable<RecipeId>>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>>(
                recipes.ToDictionary(pair => pair.Key, pair => TestRecipeOverview.From(pair.Value, call.ArgAt<UserId>(1)))));
        return lookup;
    }
}
