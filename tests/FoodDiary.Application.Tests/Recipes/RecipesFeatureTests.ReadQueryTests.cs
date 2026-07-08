using FoodDiary.Results;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.ExploreRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesOverview;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Recipes;

public partial class RecipesFeatureTests {

    [Fact]
    public async Task GetRecentRecipesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetRecentRecipesQueryValidator();
        var query = new GetRecentRecipesQuery(Guid.Empty, 10, IncludePublic: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }


    [Fact]
    public async Task GetRecipesOverviewQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecipesOverviewQueryValidator();
        var query = new GetRecipesOverviewQuery(Guid.NewGuid(), 1, 10, Search: null, IncludePublic: true, 10);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new GetRecipeByIdQueryHandler(new OverviewRecipeReadService(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<RecipeModel> result = await handler.Handle(
            new GetRecipeByIdQuery(Guid.NewGuid(), Guid.Empty, IncludePublic: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetRecipeByIdQueryHandler(new OverviewRecipeReadService(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<RecipeModel> result = await handler.Handle(
            new GetRecipeByIdQuery(UserId: null, Guid.NewGuid(), IncludePublic: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipe-reader@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var recipe = Recipe.Create(user.Id, "Soup", servings: 1);
        var handler = new GetRecipeByIdQueryHandler(
            new OverviewRecipeReadService(recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [recipe.Id] = (recipe, 0),
            }),
            new StubUserRepository(user));

        Result<RecipeModel> result = await handler.Handle(
            new GetRecipeByIdQuery(user.Id.Value, recipe.Id.Value, IncludePublic: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
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

        var handler = new GetRecipeByIdQueryHandler(
            new OverviewRecipeReadService(recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [recipe.Id] = (recipe, 3),
            }),
            new StubUserRepository(user));

        Result<RecipeModel> result = await handler.Handle(new GetRecipeByIdQuery(user.Id.Value, recipe.Id.Value, IncludePublic: false), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(recipe.Id.Value, result.Value.Id);
        Assert.Equal(3, result.Value.UsageCount);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Private note", result.Value.Comment);
    }


    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(
            new OverviewRecipeReadService(),
            new StubRecentItemRepository([]),
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(User.Create("overview-missing-user@example.com", "hash")));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(UserId: null, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WhenUserAccessFails_ReturnsAccessFailure() {
        var user = User.Create("overview-inactive-user@example.com", "hash");
        user.Deactivate();
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(
            new OverviewRecipeReadService(),
            new StubRecentItemRepository([]),
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
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

        var overviewReadService = new OverviewRecipeReadService(
            pagedItems: [(breakfast, 2), (dinner, 5)],
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [dinner.Id] = (dinner, 5),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(dinner.Id, 5, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteRecipeRepository([favorite]);
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(overviewReadService, recentRepository, favoriteRepository, new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
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
    public async Task GetRecipesOverviewQueryHandler_WhenThereAreNoRecentRecipes_ReturnsEmptyRecentItems() {
        var user = User.Create("overview-no-recents@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "No Recents Soup", servings: 1);
        recipe.AddStep(1, "Cook");
        var recentRepository = new StubRecentItemRepository([]);
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(
            new OverviewRecipeReadService(pagedItems: [(recipe, 1)]),
            recentRepository,
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(1, recentRepository.GetRecentRecipesCallCount);
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

        var overviewReadService = new OverviewRecipeReadService(pagedItems: [(recipe, 1)]);
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(recipe.Id, 1, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteRecipeRepository([]);
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(overviewReadService, recentRepository, favoriteRepository, new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, "protein", IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(0, recentRepository.GetRecentRecipesCallCount);
    }


    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithHasImageFilter_FiltersRecentItems() {
        var user = User.Create("overview-recipe-image-filter@example.com", "hash");
        var withImage = Recipe.Create(user.Id, "Photo Soup", servings: 1, imageUrl: "https://cdn.test/soup.jpg", visibility: Visibility.Private);
        var withoutImage = Recipe.Create(user.Id, "Plain Soup", servings: 1, visibility: Visibility.Private);
        var overviewReadService = new OverviewRecipeReadService(
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [withImage.Id] = (withImage, 3),
                [withoutImage.Id] = (withoutImage, 2),
            });
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(
            overviewReadService,
            new StubRecentItemRepository([
                new RecentRecipeUsage(withImage.Id, 3, DateTime.UtcNow),
                new RecentRecipeUsage(withoutImage.Id, 2, DateTime.UtcNow),
            ]),
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, HasImage: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        RecipeModel recent = Assert.Single(result.Value.RecentItems);
        Assert.Equal(withImage.Id.Value, recent.Id);
    }


    [Fact]
    public async Task GetRecentRecipesQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetRecentRecipesQueryHandler handler = CreateRecentRecipesHandler(new StubRecentItemRepository([]), new OverviewRecipeReadService());

        Result<IReadOnlyList<RecipeModel>> result = await handler.Handle(new GetRecentRecipesQuery(UserId: null, 10, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetRecentRecipesQueryHandler_WhenNoRecentRecipes_ReturnsEmptyList() {
        var userId = UserId.New();
        var recentRepository = new StubRecentItemRepository([]);
        GetRecentRecipesQueryHandler handler = CreateRecentRecipesHandler(recentRepository, new OverviewRecipeReadService());

        Result<IReadOnlyList<RecipeModel>> result = await handler.Handle(new GetRecentRecipesQuery(userId.Value, 10, IncludePublic: true), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
        Assert.Equal(1, recentRepository.GetRecentRecipesCallCount);
    }


    [Fact]
    public async Task GetRecentRecipesQueryHandler_ReturnsRecipesInRecentOrderAndSkipsMissingItems() {
        var userId = UserId.New();
        var owned = Recipe.Create(
            userId,
            "Owned Soup",
            servings: 2,
            category: "Lunch",
            visibility: Visibility.Private);
        owned.AddStep(1, "Cook soup");
        var publicRecipe = Recipe.Create(
            UserId.New(),
            "Public Pancakes",
            servings: 3,
            category: "Breakfast",
            visibility: Visibility.Public);
        publicRecipe.AddStep(1, "Cook pancakes");
        var missingRecipeId = RecipeId.New();
        var readService = new OverviewRecipeReadService(
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [owned.Id] = (owned, 5),
                [publicRecipe.Id] = (publicRecipe, 2),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(publicRecipe.Id, 2, DateTime.UtcNow),
            new RecentRecipeUsage(missingRecipeId, 9, DateTime.UtcNow),
            new RecentRecipeUsage(owned.Id, 5, DateTime.UtcNow),
        ]);
        GetRecentRecipesQueryHandler handler = CreateRecentRecipesHandler(recentRepository, readService);

        Result<IReadOnlyList<RecipeModel>> result = await handler.Handle(new GetRecentRecipesQuery(userId.Value, 99, IncludePublic: true), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal([publicRecipe.Id.Value, owned.Id.Value], [.. result.Value.Select(x => x.Id)]);
        Assert.False(result.Value[0].IsOwnedByCurrentUser);
        Assert.True(result.Value[1].IsOwnedByCurrentUser);
        Assert.Equal(2, result.Value[0].UsageCount);
        Assert.Equal(5, result.Value[1].UsageCount);
    }


    [Fact]
    public async Task ExploreRecipesQueryHandler_ReturnsPagedPublicRecipesAndOwnerFlags() {
        var user = User.Create("explore-recipes@example.com", "hash");
        var owned = Recipe.Create(user.Id, "Owned Public Soup", servings: 2, visibility: Visibility.Public);
        owned.AddStep(1, "Cook");
        var publicRecipe = Recipe.Create(UserId.New(), "Shared Salad", servings: 1, visibility: Visibility.Public);
        publicRecipe.AddStep(1, "Mix");
        var readService = new OverviewRecipeReadService(pagedItems: [(owned, 3), (publicRecipe, 7)]);
        var handler = new ExploreRecipesQueryHandler(readService);

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(
            new ExploreRecipesQuery(user.Id.Value, Page: 0, Limit: 0, Search: "s", Category: "Lunch", MaxPrepTime: 20, SortBy: "popular"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.True(result.Value.Data[0].IsOwnedByCurrentUser);
        Assert.False(result.Value.Data[1].IsOwnedByCurrentUser);
        Assert.Equal([owned.Id.Value, publicRecipe.Id.Value], [.. result.Value.Data.Select(x => x.Id)]);
    }


    [Fact]
    public async Task ExploreRecipesQueryHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ExploreRecipesQueryHandler(new OverviewRecipeReadService());

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(
            new ExploreRecipesQuery(Guid.Empty, Page: 1, Limit: 10, Search: null, Category: null, MaxPrepTime: null, SortBy: "popular"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task GetRecipesQueryHandler_WithValidQuery_ReturnsPagedRecipeModels() {
        var user = User.Create("recipes-list@example.com", "hash");
        var ownedRecipe = Recipe.Create(user.Id, "Owned soup", servings: 2, comment: "Private note", visibility: Visibility.Private);
        ownedRecipe.SetManualNutrition(200, 10, 5, 20, 2, 0);
        var publicOwnerId = UserId.New();
        var publicRecipe = Recipe.Create(publicOwnerId, "Public salad", servings: 1, visibility: Visibility.Public);
        var repository = new OverviewRecipeReadService([
            (ownedRecipe, 3),
            (publicRecipe, 5),
        ]);
        var handler = new GetRecipesQueryHandler(repository, new StubUserRepository(user));

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(
            new GetRecipesQuery(user.Id.Value, Page: 0, Limit: 0, Search: "s", IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Data.Count);
        RecipeModel owned = result.Value.Data.Single(recipe => recipe.Id == ownedRecipe.Id.Value);
        Assert.True(owned.IsOwnedByCurrentUser);
        Assert.Equal("Private note", owned.Comment);
        Assert.Equal(3, owned.UsageCount);
        RecipeModel publicModel = result.Value.Data.Single(recipe => recipe.Id == publicRecipe.Id.Value);
        Assert.False(publicModel.IsOwnedByCurrentUser);
        Assert.Null(publicModel.Comment);
        Assert.Equal(5, publicModel.UsageCount);
    }


    [Fact]
    public async Task GetRecipesQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetRecipesQueryHandler(
            new OverviewRecipeReadService(),
            new StubUserRepository(User.Create("unused@example.com", "hash")));

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(new GetRecipesQuery(Guid.Empty, 1, 10, Search: null, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetRecipesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipes-list@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetRecipesQueryHandler(new OverviewRecipeReadService(), new StubUserRepository(user));

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(new GetRecipesQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

}
