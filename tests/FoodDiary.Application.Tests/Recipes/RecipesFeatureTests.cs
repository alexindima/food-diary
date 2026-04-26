using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesOverview;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.FavoriteRecipes;
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
    public async Task GetRecipesOverviewQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecipesOverviewQueryValidator();
        var query = new GetRecipesOverviewQuery(Guid.NewGuid(), 1, 10, null, true, 10);

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
    public async Task CreateRecipeCommandHandler_WithValidCommand_PersistsAndReturnsOwnedModel() {
        var user = User.Create("create-recipe@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(user));

        var result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Tomato Soup",
                Description: "Creamy soup",
                Comment: "Serve warm",
                Category: "Dinner",
                ImageUrl: "https://cdn.test/soup.png",
                ImageAssetId: null,
                PrepTime: 15,
                CookTime: 30,
                Servings: 4,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: 320,
                ManualProteins: 14,
                ManualFats: 9,
                ManualCarbs: 40,
                ManualFiber: 6,
                ManualAlcohol: 0,
                Steps: [
                    CreateRecipeCreateStep(order: 1, "Chop vegetables"),
                    CreateRecipeCreateStep(order: 2, "Boil soup")
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.Equal("Tomato Soup", repository.LastAddedRecipe.Name);
        Assert.Equal(2, repository.LastAddedRecipe.Steps.Count);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Serve warm", result.Value.Comment);
        Assert.Equal(2, result.Value.Steps.Count);
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
    public async Task GetRecipeByIdQueryHandler_WithAccessibleRecipe_ReturnsUsageAndOwnerComment() {
        var user = User.Create("recipe-by-id@example.com", "hash");
        var recipe = Recipe.Create(
            user.Id,
            "Chicken Soup",
            servings: 3,
            description: "Rich broth",
            comment: "Private note",
            category: "Lunch",
            visibility: Visibility.Private);
        recipe.AddStep(1, "Prepare ingredients");
        SetRecipeUsageCollections(recipe, mealItemsCount: 2, nestedRecipeUsageCount: 1);

        var handler = new GetRecipeByIdQueryHandler(new SingleRecipeRepository(recipe), new StubUserRepository(user));

        var result = await handler.Handle(new GetRecipeByIdQuery(user.Id.Value, recipe.Id.Value, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(recipe.Id.Value, result.Value.Id);
        Assert.Equal(3, result.Value.UsageCount);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Private note", result.Value.Comment);
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
    public async Task DuplicateRecipeCommandHandler_WithExistingRecipe_CopiesFieldsAndClearsImageAsset() {
        var user = User.Create("duplicate-recipe@example.com", "hash");
        var nestedRecipeId = RecipeId.New();
        var original = Recipe.Create(
            user.Id,
            "Original Soup",
            servings: 2,
            description: "Rich soup",
            comment: "Original note",
            category: "Dinner",
            imageUrl: "https://cdn.test/original-soup.png",
            imageAssetId: ImageAssetId.New(),
            prepTime: 15,
            cookTime: 35,
            visibility: Visibility.Public);
        var step = original.AddStep(1, "Boil water", "Prep", "https://cdn.test/step.png", ImageAssetId.New());
        step.AddProductIngredient(ProductId.New(), 200);
        step.AddNestedRecipeIngredient(nestedRecipeId, 50);

        var repository = new SingleRecipeRepository(original);
        var handler = new DuplicateRecipeCommandHandler(repository);

        var result = await handler.Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.NotEqual(original.Id, repository.LastAddedRecipe.Id);
        Assert.Equal(original.Name, repository.LastAddedRecipe.Name);
        Assert.Equal(original.ImageUrl, repository.LastAddedRecipe.ImageUrl);
        Assert.Null(repository.LastAddedRecipe.ImageAssetId);
        Assert.Equal(user.Id, repository.LastAddedRecipe.UserId);
        Assert.Single(repository.LastAddedRecipe.Steps);
        Assert.Equal(2, repository.LastAddedRecipe.Steps.Single().Ingredients.Count);
        Assert.True(result.Value.IsOwnedByCurrentUser);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithoutSearch_ReturnsRecentFavoritesAndFavoriteFlags() {
        var user = User.Create("overview-recipes@example.com", "hash");
        var breakfast = Recipe.Create(
            user.Id,
            "Breakfast Bowl",
            servings: 1,
            category: "Breakfast",
            visibility: Visibility.Private);
        breakfast.AddStep(1, "Mix ingredients");

        var dinner = Recipe.Create(
            user.Id,
            "Dinner Soup",
            servings: 2,
            category: "Dinner",
            visibility: Visibility.Private);
        dinner.AddStep(1, "Cook soup");

        var favorite = FavoriteRecipe.Create(user.Id, dinner.Id, "Fav dinner");
        SetFavoriteRecipeNavigation(favorite, dinner);

        var repository = new OverviewRecipeRepository(
            pagedItems: [(breakfast, 2), (dinner, 5)],
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [dinner.Id] = (dinner, 5),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(dinner.Id, 5, DateTime.UtcNow)
        ]);
        var favoriteRepository = new StubFavoriteRecipeRepository([favorite]);
        var handler = new GetRecipesOverviewQueryHandler(repository, recentRepository, favoriteRepository);

        var result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, null, true, 10, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.AllRecipes.Data.Count);
        Assert.Single(result.Value.RecentItems);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.Equal(dinner.Id.Value, result.Value.RecentItems[0].Id);
        Assert.True(result.Value.RecentItems[0].IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.RecentItems[0].FavoriteRecipeId);
        Assert.True(result.Value.AllRecipes.Data.Single(x => x.Id == dinner.Id.Value).IsFavorite);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithSearch_SkipsRecentItems() {
        var user = User.Create("overview-search-recipe@example.com", "hash");
        var recipe = Recipe.Create(
            user.Id,
            "Protein Pancakes",
            servings: 2,
            category: "Breakfast",
            visibility: Visibility.Private);
        recipe.AddStep(1, "Cook pancakes");

        var repository = new OverviewRecipeRepository(pagedItems: [(recipe, 1)]);
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(recipe.Id, 1, DateTime.UtcNow)
        ]);
        var favoriteRepository = new StubFavoriteRecipeRepository([]);
        var handler = new GetRecipesOverviewQueryHandler(repository, recentRepository, favoriteRepository);

        var result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, "protein", true, 10, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(0, recentRepository.GetRecentRecipesCallCount);
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
        public Recipe? LastAddedRecipe { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            LastAddedRecipe = recipe;
            return Task.FromResult(recipe);
        }

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
            Task.FromResult<Recipe?>(
                LastAddedRecipe is not null && LastAddedRecipe.Id == id && LastAddedRecipe.UserId == userId
                    ? LastAddedRecipe
                    : id == recipe.Id && userId == recipe.UserId
                        ? recipe
                        : null);

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
        public Recipe? LastAddedRecipe { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            LastAddedRecipe = recipe;
            return Task.FromResult(recipe);
        }

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
            Task.FromResult<Recipe?>(LastAddedRecipe is not null && LastAddedRecipe.Id == id ? LastAddedRecipe : null);

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

    private sealed class OverviewRecipeRepository(
        IReadOnlyList<(Recipe Recipe, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>? recipesByIdWithUsage = null) : IRecipeRepository {
        private readonly IReadOnlyList<(Recipe Recipe, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)> _recipesByIdWithUsage = recipesByIdWithUsage ?? new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)>();

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((_pagedItems, _pagedItems.Count));

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(_pagedItems.Select(x => x.Recipe).FirstOrDefault(x => x.Id == id && x.UserId == userId));

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(new Dictionary<RecipeId, Recipe>());

        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            var idSet = ids.ToHashSet();
            var filtered = _recipesByIdWithUsage
                .Where(pair => idSet.Contains(pair.Key))
                .ToDictionary();
            return Task.FromResult<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>>(filtered);
        }

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRecentItemRepository(IReadOnlyList<RecentRecipeUsage> recentRecipes) : IRecentItemRepository {
        private readonly IReadOnlyList<RecentRecipeUsage> _recentRecipes = recentRecipes;
        public int GetRecentRecipesCallCount { get; private set; }

        public Task RegisterUsageAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            IReadOnlyCollection<RecipeId> recipeIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) {
            GetRecentRecipesCallCount++;
            return Task.FromResult<IReadOnlyList<RecentRecipeUsage>>(_recentRecipes.Take(limit).ToList());
        }
    }

    private sealed class StubFavoriteRecipeRepository(IReadOnlyList<FavoriteRecipe> favorites) : IFavoriteRecipeRepository {
        private readonly IReadOnlyList<FavoriteRecipe> _favorites = favorites;

        public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteRecipe?> GetByIdAsync(FavoriteRecipeId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteRecipe?> GetByRecipeIdAsync(RecipeId recipeId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(UserId userId, IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, FavoriteRecipe>>(_favorites.Where(f => recipeIds.Contains(f.RecipeId)).ToDictionary(f => f.RecipeId));
        public Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites);
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

    private static void SetFavoriteRecipeNavigation(FavoriteRecipe favorite, Recipe recipe) {
        typeof(FavoriteRecipe)
            .GetProperty(nameof(FavoriteRecipe.Recipe))!
            .SetValue(favorite, recipe);
    }

    private static void SetRecipeUsageCollections(Recipe recipe, int mealItemsCount, int nestedRecipeUsageCount) {
        var mealItems = Enumerable.Range(0, mealItemsCount)
            .Select(_ => (FoodDiary.Domain.Entities.Meals.MealItem)null!)
            .ToList();
        var nestedRecipeUsages = Enumerable.Range(0, nestedRecipeUsageCount)
            .Select(_ => (RecipeIngredient)null!)
            .ToList();

        typeof(Recipe)
            .GetField("_mealItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(recipe, mealItems);
        typeof(Recipe)
            .GetField("_nestedRecipeUsages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(recipe, nestedRecipeUsages);
    }
}
