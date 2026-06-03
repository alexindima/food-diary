using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class UpdateRecipeCommandHandlerTests {
    [Fact]
    public async Task Handle_WithDuplicateStepOrder_ThrowsArgumentException() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var command = new UpdateRecipeCommand(
            userId.Value,
            recipeId.Value,
            Name: "Updated soup",
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            Category: null,
            ClearCategory: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: null,
            ClearImageAssetId: false,
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            Visibility: Visibility.Public.ToString(),
            CalculateNutritionAutomatically: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            Steps: [
                CreateStep(order: 1, "Step 1"),
                CreateStep(order: 1, "Step 2 duplicate order")
            ]);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithClearImageFlags_ClearsRecipeMedia() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var imageAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, imageUrl: "https://img", imageAssetId: imageAssetId);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var command = new UpdateRecipeCommand(
            userId.Value,
            recipeId.Value,
            Name: "Soup",
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            Category: null,
            ClearCategory: false,
            ImageUrl: null,
            ClearImageUrl: true,
            ImageAssetId: null,
            ClearImageAssetId: true,
            PrepTime: 0,
            CookTime: 20,
            Servings: 2,
            Visibility: Visibility.Public.ToString(),
            CalculateNutritionAutomatically: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            Steps: [CreateStep(order: 1, "Initial step")]);

        await handler.Handle(command, CancellationToken.None);

        Assert.Null(recipe.ImageUrl);
        Assert.Null(recipe.ImageAssetId);
    }

    [Fact]
    public async Task Handle_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: 0,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: null,
                ManualProteins: 10,
                ManualFats: 4,
                ManualCarbs: 20,
                ManualFiber: 2,
                ManualAlcohol: 0,
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("calories", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: Guid.Empty,
                ClearImageAssetId: false,
                PrepTime: 0,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyNestedRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: 0,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Initial step",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: null,
                    Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: Guid.Empty, Amount: 100)])]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("NestedRecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);

        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(RecipeId.New(), userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                Guid.Empty,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: 0,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithSelfNestedRecipeIngredient_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: 0,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateStepWithNestedRecipe(order: 1, "Initial step", recipeId.Value)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("itself", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WhenPrepTimeOmitted_PreservesExistingPrepTime() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, prepTime: 15);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");

        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: null,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, recipe.PrepTime);
    }

    private static RecipeStepInput CreateStep(int order, string description) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)]);
    }

    private static RecipeStepInput CreateStepWithNestedRecipe(int order, string description, Guid nestedRecipeId) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: nestedRecipeId, Amount: 1)]);
    }

    private static void SetRecipeId(Recipe recipe, RecipeId recipeId) {
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRecipeRepository : IRecipeRepository {
        private readonly RecipeId _recipeId;
        private readonly UserId _userId;
        private readonly Recipe _recipe;

        public StubRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe) {
            _recipeId = recipeId;
            _userId = userId;
            _recipe = recipe;
        }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) {
            if (id == _recipeId && userId == _userId) {
                return Task.FromResult<Recipe?>(_recipe);
            }

            return Task.FromResult<Recipe?>(null);
        }

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopImageAssetCleanupService : IImageAssetCleanupService {
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(true));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowAllProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(
                ids.Distinct().ToDictionary(id => id, id => CreateProduct(userId, id)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowAllRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(
                ids.Distinct().ToDictionary(id => id, id => CreateRecipe(userId, id)));
    }

    private static Product CreateProduct(UserId userId, ProductId productId) {
        var product = Product.Create(userId, "Ingredient", MeasurementUnit.G, 100, null, 100, 1, 1, 1, 1, 0);
        typeof(Product)
            .GetProperty(nameof(Product.Id))!
            .SetValue(product, productId);
        return product;
    }

    private static Recipe CreateRecipe(UserId userId, RecipeId recipeId) {
        var recipe = Recipe.Create(userId, "Nested", servings: 1);
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
        return recipe;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
