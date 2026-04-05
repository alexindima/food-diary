using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public class RecipesFeatureTests {
    [Fact]
    public async Task GetRecentRecipesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetRecentRecipesQueryValidator();
        var query = new GetRecentRecipesQuery(Guid.Empty, 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecipesWithRecentQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecipesWithRecentQueryValidator();
        var query = new GetRecipesWithRecentQuery(Guid.NewGuid(), 1, 10, null, true, 10);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WhenCleanupFails_StillDeletesRecipeAndReturnsSuccess() {
        var userId = UserId.New();
        var recipeAssetId = ImageAssetId.New();
        var stepAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(
            userId,
            name: "Soup",
            servings: 2,
            imageAssetId: recipeAssetId,
            visibility: Visibility.Private);
        recipe.AddStep(1, "Prepare ingredients", imageAssetId: stepAssetId);

        var repository = new SingleRecipeRepository(recipe);
        var cleanup = new RecordingCleanupService("storage_error");
        var handler = new DeleteRecipeCommandHandler(repository, cleanup);

        var result = await handler.Handle(new DeleteRecipeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([recipeAssetId, stepAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new DeleteRecipeCommandHandler(
            new SingleRecipeRepository(Recipe.Create(UserId.New(), "Soup", servings: 2)),
            new RecordingCleanupService());

        var result = await handler.Handle(
            new DeleteRecipeCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-update-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var recipe = Recipe.Create(user.Id, "Soup", servings: 2, visibility: Visibility.Private);
        var handler = new UpdateRecipeCommandHandler(
            new SingleRecipeRepository(recipe),
            new RecordingCleanupService(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                user.Id.Value,
                recipe.Id.Value,
                Name: "Updated Soup",
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
                CookTime: null,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateRecipeCommandHandler_WithoutImageChange_DoesNotCleanupExistingRecipeAsset() {
        var user = User.Create("recipe-owner@example.com", "hash");
        var recipeAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(
            user.Id,
            "Soup",
            servings: 2,
            imageAssetId: recipeAssetId,
            visibility: Visibility.Private);

        var cleanup = new RecordingCleanupService();
        var handler = new UpdateRecipeCommandHandler(
            new SingleRecipeRepository(recipe),
            cleanup,
            new StubUserRepository(user));

        var result = await handler.Handle(
            new UpdateRecipeCommand(
                user.Id.Value,
                recipe.Id.Value,
                Name: "Updated Soup",
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
                CookTime: null,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: []),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: null,
                ManualProteins: 10,
                ManualFats: 4,
                ManualCarbs: 20,
                ManualFiber: 2,
                ManualAlcohol: 0,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("calories", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new GetRecipeByIdQueryHandler(new SingleRecipeRepositoryForCreate(), new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new GetRecipeByIdQuery(Guid.NewGuid(), Guid.Empty, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new DuplicateRecipeCommandHandler(new SingleRecipeRepositoryForCreate());

        var result = await handler.Handle(
            new DuplicateRecipeCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: Guid.Empty,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyStepImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Step 1",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: Guid.Empty,
                    Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyIngredientProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Step 1",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: null,
                    Ingredients: [new RecipeIngredientInput(ProductId: Guid.Empty, NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RecipeStepInput CreateRecipeCreateStep(int order, string description) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)]);
    }

    private sealed class SingleRecipeRepository(Recipe recipe) : IRecipeRepository {
        public bool DeleteCalled { get; private set; }

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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(id == recipe.Id && userId == recipe.UserId ? recipe : null);

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

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class SingleRecipeRepositoryForCreate : IRecipeRepository {
        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.FromResult(recipe);

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
            CancellationToken cancellationToken = default) => Task.FromResult<Recipe?>(null);

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

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(true)
                : new DeleteImageAssetResult(false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(user));

        var result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

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
