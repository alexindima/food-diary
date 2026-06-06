using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;
using FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.FavoriteRecipes;

[ExcludeFromCodeCoverage]
public sealed class FavoriteRecipesAdditionalFeatureTests {
    [Fact]
    public async Task AddFavoriteRecipe_WithAccessibleRecipe_PersistsAndReturnsModel() {
        var user = User.Create("favorite-recipe@example.com", "hash");
        Recipe recipe = CreateRecipe(user.Id, "Tomato Soup");
        var favoriteRepository = new InMemoryFavoriteRecipeRepository(recipe);
        var handler = new AddFavoriteRecipeCommandHandler(
            favoriteRepository,
            new SingleRecipeRepository(recipe),
            new SingleUserRepository(user));

        Result<FavoriteRecipeModel> result = await handler.Handle(
            new AddFavoriteRecipeCommand(user.Id.Value, recipe.Id.Value, "Dinner"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(favoriteRepository.AddedFavorite);
        Assert.Equal(recipe.Id.Value, result.Value.RecipeId);
        Assert.Equal("Tomato Soup", result.Value.RecipeName);
        Assert.Equal("Dinner", result.Value.Name);
    }

    [Fact]
    public async Task AddFavoriteRecipe_WhenRecipeMissing_ReturnsNotFound() {
        var user = User.Create("missing-favorite-recipe@example.com", "hash");
        var handler = new AddFavoriteRecipeCommandHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleRecipeRepository(null),
            new SingleUserRepository(user));

        Result<FavoriteRecipeModel> result = await handler.Handle(
            new AddFavoriteRecipeCommand(user.Id.Value, Guid.NewGuid(), "Missing"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Recipe.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteRecipe_WhenAlreadyExists_ReturnsFailure() {
        var user = User.Create("duplicate-favorite-recipe@example.com", "hash");
        Recipe recipe = CreateRecipe(user.Id, "Soup");
        var existing = FavoriteRecipe.Create(user.Id, recipe.Id, "Existing");
        SetRecipeNavigation(existing, recipe);
        var handler = new AddFavoriteRecipeCommandHandler(
            new InMemoryFavoriteRecipeRepository(recipe, [existing]),
            new SingleRecipeRepository(recipe),
            new SingleUserRepository(user));

        Result<FavoriteRecipeModel> result = await handler.Handle(
            new AddFavoriteRecipeCommand(user.Id.Value, recipe.Id.Value, "Again"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FavoriteRecipe.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteRecipe_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new AddFavoriteRecipeCommandHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleRecipeRepository(null),
            new SingleUserRepository(User.Create("invalid-add-favorite-recipe@example.com", "hash")));

        Result<FavoriteRecipeModel> result = await handler.Handle(
            new AddFavoriteRecipeCommand(Guid.Empty, Guid.NewGuid(), "Invalid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteRecipe_WhenUserDeleted_ReturnsAccessFailure() {
        var user = User.Create("deleted-add-favorite-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        Recipe recipe = CreateRecipe(user.Id, "Deleted User Soup");
        var handler = new AddFavoriteRecipeCommandHandler(
            new InMemoryFavoriteRecipeRepository(recipe),
            new SingleRecipeRepository(recipe),
            new SingleUserRepository(user));

        Result<FavoriteRecipeModel> result = await handler.Handle(
            new AddFavoriteRecipeCommand(user.Id.Value, recipe.Id.Value, "Dinner"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteRecipe_WhenUserMissing_ReturnsInvalidToken() {
        Recipe recipe = CreateRecipe(UserId.New(), "Missing User Soup");
        var handler = new AddFavoriteRecipeCommandHandler(
            new InMemoryFavoriteRecipeRepository(recipe),
            new SingleRecipeRepository(recipe),
            new SingleUserRepository(null));

        Result<FavoriteRecipeModel> result = await handler.Handle(
            new AddFavoriteRecipeCommand(Guid.NewGuid(), recipe.Id.Value, "Dinner"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteRecipes_ReturnsMappedFavorites() {
        var user = User.Create("get-favorite-recipes@example.com", "hash");
        Recipe recipe = CreateRecipe(user.Id, "Chicken Soup");
        var favorite = FavoriteRecipe.Create(user.Id, recipe.Id, "Lunch");
        SetRecipeNavigation(favorite, recipe);
        var handler = new GetFavoriteRecipesQueryHandler(
            new InMemoryFavoriteRecipeRepository(recipe, [favorite]),
            new SingleUserRepository(user));

        Result<IReadOnlyList<FavoriteRecipeModel>> result = await handler.Handle(new GetFavoriteRecipesQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Chicken Soup", result.Value[0].RecipeName);
    }

    [Fact]
    public async Task GetFavoriteRecipes_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetFavoriteRecipesQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(User.Create("invalid-get-favorite-recipes@example.com", "hash")));

        Result<IReadOnlyList<FavoriteRecipeModel>> result = await handler.Handle(new GetFavoriteRecipesQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteRecipes_WhenUserDeleted_ReturnsAccessFailure() {
        var user = User.Create("deleted-get-favorite-recipes@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetFavoriteRecipesQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(user));

        Result<IReadOnlyList<FavoriteRecipeModel>> result = await handler.Handle(new GetFavoriteRecipesQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteRecipes_WhenUserMissing_ReturnsInvalidToken() {
        var handler = new GetFavoriteRecipesQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(null));

        Result<IReadOnlyList<FavoriteRecipeModel>> result = await handler.Handle(new GetFavoriteRecipesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task IsRecipeFavorite_ReturnsFalseWhenFavoriteMissing() {
        var user = User.Create("is-favorite-recipe@example.com", "hash");
        var handler = new IsRecipeFavoriteQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsRecipeFavoriteQuery(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task IsRecipeFavorite_WhenUserDeleted_ReturnsAccessFailure() {
        var user = User.Create("deleted-is-favorite-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new IsRecipeFavoriteQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsRecipeFavoriteQuery(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task IsRecipeFavorite_WhenUserMissing_ReturnsInvalidToken() {
        var handler = new IsRecipeFavoriteQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(null));

        Result<bool> result = await handler.Handle(
            new IsRecipeFavoriteQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task IsRecipeFavorite_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new IsRecipeFavoriteQueryHandler(
            new InMemoryFavoriteRecipeRepository(),
            new SingleUserRepository(User.Create("invalid-is-favorite-recipe@example.com", "hash")));

        Result<bool> result = await handler.Handle(
            new IsRecipeFavoriteQuery(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task RemoveFavoriteRecipe_DeletesExistingFavorite() {
        var user = User.Create("remove-favorite-recipe@example.com", "hash");
        Recipe recipe = CreateRecipe(user.Id, "Pear Tart");
        var favorite = FavoriteRecipe.Create(user.Id, recipe.Id, "Dessert");
        SetRecipeNavigation(favorite, recipe);
        var repository = new InMemoryFavoriteRecipeRepository(recipe, [favorite]);
        var handler = new RemoveFavoriteRecipeCommandHandler(repository, new SingleUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteRecipeCommand(user.Id.Value, favorite.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteRecipe_WhenFavoriteMissing_ReturnsNotFound() {
        var user = User.Create("missing-remove-favorite-recipe@example.com", "hash");
        var repository = new InMemoryFavoriteRecipeRepository();
        var handler = new RemoveFavoriteRecipeCommandHandler(repository, new SingleUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteRecipeCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FavoriteRecipe.NotFound", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteRecipe_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryFavoriteRecipeRepository();
        var handler = new RemoveFavoriteRecipeCommandHandler(
            repository,
            new SingleUserRepository(User.Create("invalid-remove-favorite-recipe@example.com", "hash")));

        Result result = await handler.Handle(
            new RemoveFavoriteRecipeCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteRecipe_WhenUserDeleted_ReturnsAccessFailure() {
        var user = User.Create("deleted-remove-favorite-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        Recipe recipe = CreateRecipe(user.Id, "Deleted User Tart");
        var favorite = FavoriteRecipe.Create(user.Id, recipe.Id, "Dessert");
        SetRecipeNavigation(favorite, recipe);
        var repository = new InMemoryFavoriteRecipeRepository(recipe, [favorite]);
        var handler = new RemoveFavoriteRecipeCommandHandler(repository, new SingleUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteRecipeCommand(user.Id.Value, favorite.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteRecipe_WhenUserMissing_ReturnsInvalidToken() {
        var userId = Guid.NewGuid();
        Recipe recipe = CreateRecipe(new UserId(userId), "Missing User Tart");
        var favorite = FavoriteRecipe.Create(new UserId(userId), recipe.Id, "Dessert");
        SetRecipeNavigation(favorite, recipe);
        var repository = new InMemoryFavoriteRecipeRepository(recipe, [favorite]);
        var handler = new RemoveFavoriteRecipeCommandHandler(repository, new SingleUserRepository(null));

        Result result = await handler.Handle(
            new RemoveFavoriteRecipeCommand(userId, favorite.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    private static Recipe CreateRecipe(UserId userId, string name) {
        var recipe = Recipe.Create(userId, name, servings: 2, prepTime: 10, cookTime: 20, visibility: Visibility.Private);
        recipe.AddStep(1, "Cook");
        return recipe;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryFavoriteRecipeRepository(
        Recipe? recipe = null,
        IReadOnlyList<FavoriteRecipe>? favorites = null) : IFavoriteRecipeRepository {
        private readonly List<FavoriteRecipe> _favorites = favorites?.ToList() ?? [];
        public FavoriteRecipe? AddedFavorite { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
            if (recipe is not null) {
                SetRecipeNavigation(favorite, recipe);
            }

            AddedFavorite = favorite;
            _favorites.Add(favorite);
            return Task.FromResult(favorite);
        }

        public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            _favorites.Remove(favorite);
            return Task.CompletedTask;
        }

        public Task<FavoriteRecipe?> GetByIdAsync(
            FavoriteRecipeId id,
            UserId userId,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites.FirstOrDefault(f => f.Id == id && f.UserId == userId));

        public Task<FavoriteRecipe?> GetByRecipeIdAsync(
            RecipeId recipeId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites.FirstOrDefault(f => f.RecipeId == recipeId && f.UserId == userId));

        public Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteRecipe>>(_favorites.Where(f => f.UserId == userId).ToList());

        public Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(
            UserId userId,
            IReadOnlyCollection<RecipeId> recipeIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, FavoriteRecipe>>(
                _favorites.Where(f => f.UserId == userId && recipeIds.Contains(f.RecipeId)).ToDictionary(f => f.RecipeId));
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleRecipeRepository(Recipe? recipe) : IRecipeRepository {
        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(UserId userId, bool includePublic, int page, int limit, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Recipe?> GetByIdAsync(RecipeId id, UserId userId, bool includePublic = true, bool includeSteps = false, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult(recipe is not null && recipe.Id == id ? recipe : null);
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(IEnumerable<RecipeId> ids, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(IEnumerable<RecipeId> ids, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static void SetRecipeNavigation(FavoriteRecipe favorite, Recipe recipe) {
        typeof(FavoriteRecipe)
            .GetProperty(nameof(FavoriteRecipe.Recipe))!
            .SetValue(favorite, recipe);
    }
}
