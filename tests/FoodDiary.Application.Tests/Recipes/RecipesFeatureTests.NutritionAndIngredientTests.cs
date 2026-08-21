using FoodDiary.Results;
using FoodDiary.Application.Recipes.Recipes.Common;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Recipes.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public partial class RecipesFeatureTests {

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithMissingProduct_ReturnsValidationFailure() {
        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [CreateRecipeStepWithProduct(order: 1, "Mix", Guid.NewGuid())],
            recipeId: null,
            UserId.New(),
            new EmptyProductLookupService(),
            new AllowAllRecipeLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Product", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithSelfReference_ReturnsValidationFailure() {
        var recipeId = RecipeId.New();
        var step = new RecipeStepInput(
            Order: 1,
            Description: "Mix",
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: recipeId.Value, Amount: 1)]);

        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [step],
            recipeId,
            UserId.New(),
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("itself", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithIndirectCycle_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var nestedRecipe = Recipe.Create(userId, "Nested", servings: 1);
        nestedRecipe.AddStep(1, "Mix").AddNestedRecipeIngredient(recipeId, 1);
        var lookup = new GraphRecipeLookupService([
            TestRecipeOverview.From(nestedRecipe, userId),
        ]);
        var step = new RecipeStepInput(
            Order: 1,
            Description: "Mix",
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: nestedRecipe.Id.Value, Amount: 1)]);

        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [step],
            recipeId,
            userId,
            new AllowAllProductLookupService(),
            lookup,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("circular", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithMissingNestedRecipe_ReturnsValidationFailure() {
        var step = new RecipeStepInput(
            Order: 1,
            Description: "Mix",
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: Guid.NewGuid(), Amount: 1)]);

        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [step],
            recipeId: null,
            UserId.New(),
            new AllowAllProductLookupService(),
            new EmptyRecipeLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Nested recipe", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RecipeIngredientAccessValidator_WithTransitiveDependency_ValidatesWholeGraph(bool dependencyExists) {
        var userId = UserId.New();
        var editedRecipeId = RecipeId.New();
        var firstDependency = Recipe.Create(userId, "First", servings: 1);
        var secondDependency = Recipe.Create(userId, "Second", servings: 1);
        firstDependency.AddStep(1, "Mix").AddNestedRecipeIngredient(secondDependency.Id, 1);
        IReadOnlyList<RecipeOverviewReadItem> recipes = dependencyExists
            ? [TestRecipeOverview.From(firstDependency, userId), TestRecipeOverview.From(secondDependency, userId)]
            : [TestRecipeOverview.From(firstDependency, userId)];
        var lookup = new GraphRecipeLookupService(recipes);
        var step = new RecipeStepInput(
            Order: 1,
            Description: "Mix",
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: firstDependency.Id.Value, Amount: 1)]);

        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [step],
            editedRecipeId,
            userId,
            new AllowAllProductLookupService(),
            lookup,
            CancellationToken.None);

        Assert.Equal(dependencyExists, result.IsSuccess);
        if (!dependencyExists) {
            Assert.Contains("dependency not found", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RecipeNutritionUpdater_WhenManualNutrition_DoesNotUpdateRepository() {
        var recipe = Recipe.Create(UserId.New(), "Manual", servings: 1);
        recipe.SetManualNutrition(100, 10, 1, 2, 3, 0);
        var repository = new RecordingRecipeNutritionRepository();

        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, repository, CancellationToken.None);

        Assert.Equal(0, repository.UpdateNutritionCallCount);
    }

    [Fact]
    public async Task RecipeNutritionUpdater_WhenAutoNutritionChanged_UpdatesComputedNutrition() {
        var userId = UserId.New();
        var product = Product.Create(
            userId,
            "Ingredient",
            MeasurementUnit.G,
            100,
            defaultPortionAmount: null,
            200,
            10,
            5,
            20,
            4,
            0);
        var recipe = Recipe.Create(userId, "Auto", servings: 1);
        recipe.ApplyComputedNutrition(1, 1, 1, 1, 1, 1);
        RecipeStep step = recipe.AddStep(1, "Mix");
        step.AddProductIngredient(product.Id, 100);
        RecipeIngredient ingredient = Assert.Single(step.Ingredients);
        typeof(RecipeIngredient)
            .GetProperty(nameof(RecipeIngredient.Product))!
            .SetValue(ingredient, product);
        var repository = new RecordingRecipeNutritionRepository();

        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, repository, CancellationToken.None);

        Assert.Equal(1, repository.UpdateNutritionCallCount);
        Assert.Equal(200, recipe.TotalCalories);
        Assert.Equal(10, recipe.TotalProteins);
    }

    [Fact]
    public async Task RecipeNutritionUpdater_WhenComputedNutritionIsAlreadyClose_DoesNotUpdateRepository() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Empty Auto", servings: 1);
        recipe.ApplyComputedNutrition(0, 0, 0, 0, 0, 0);
        var repository = new RecordingRecipeNutritionRepository();

        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, repository, CancellationToken.None);

        Assert.Equal(0, repository.UpdateNutritionCallCount);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, 1d, false)]
    [InlineData(1d, null, false)]
    [InlineData(1d, 1.005d, true)]
    [InlineData(1d, 1.02d, false)]
    public void RecipeNutritionUpdater_AreClose_HandlesNullableAndToleranceBranches(double? left, double? right, bool expected) {
        System.Reflection.MethodInfo method = typeof(RecipeNutritionUpdater).GetMethod(
            "AreClose",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        bool actual = (bool)method.Invoke(null, [left, right])!;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecipeManualNutritionValidator_WhenCaloriesExceedLimit_ReturnsFailure() {
        Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> result = RecipeManualNutritionValidator.Validate(
            ManualNutritionLimits.MaxCalories + 1,
            proteins: 10,
            fats: 5,
            carbs: 20,
            fiber: 3,
            alcohol: 0);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void RecipeManualNutritionValidator_WhenNutrientExceedsLimit_ReturnsFailure() {
        Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> result = RecipeManualNutritionValidator.Validate(
            calories: 200,
            ManualNutritionLimits.MaxNutrient + 1,
            fats: 5,
            carbs: 20,
            fiber: 3,
            alcohol: 0);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [ExcludeFromCodeCoverage]
    private sealed class GraphRecipeLookupService(IEnumerable<RecipeOverviewReadItem> recipes) : IRecipeLookupService {
        private readonly IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> _recipes = recipes.ToDictionary(recipe => recipe.Id);

        public Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) {
            IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> result = ids
                .Distinct()
                .Where(_recipes.ContainsKey)
                .ToDictionary(id => id, id => _recipes[id]);
            return Task.FromResult(result);
        }
    }

}
