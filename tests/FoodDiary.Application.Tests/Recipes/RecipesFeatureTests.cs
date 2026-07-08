using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.FavoriteRecipes.Services;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesOverview;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public partial class RecipesFeatureTests {
    private static CreateRecipeCommandHandler CreateRecipeHandler(
        IRecipeRepository repository,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        IProductLookupService productLookupService,
        IRecipeLookupService recipeLookupService) =>
        new(repository, repository, currentUserAccessService, imageAssetAccessService, productLookupService, recipeLookupService);

    private static DeleteRecipeCommandHandler DeleteRecipeHandler(
        IRecipeRepository repository,
        IImageAssetCleanupService imageAssetCleanupService,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            repository,
            repository,
            imageAssetCleanupService,
            currentUserAccessService ?? new StubUserRepository(User.Create("recipe-deleter@example.com", "hash")));

    private static DuplicateRecipeCommandHandler DuplicateRecipeHandler(IRecipeRepository repository) =>
        new(repository, repository, repository, new StubUserRepository(User.Create("recipe-duplicator@example.com", "hash")));

    private static DuplicateRecipeCommandHandler DuplicateRecipeHandler(IRecipeRepository repository, ICurrentUserAccessService currentUserAccessService) =>
        new(repository, repository, repository, currentUserAccessService);

    private static UpdateRecipeCommandHandler UpdateRecipeHandler(
        IRecipeRepository repository,
        IImageAssetCleanupService imageAssetCleanupService,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        IProductLookupService productLookupService,
        IRecipeLookupService recipeLookupService) =>
        new(
            repository,
            repository,
            repository,
            imageAssetCleanupService,
            currentUserAccessService,
            imageAssetAccessService,
            productLookupService,
            recipeLookupService);

    private static GetRecipesOverviewQueryHandler CreateRecipesOverviewHandler(
        IRecipeOverviewReadService overviewReadService,
        StubRecentItemRepository recentRepository,
        StubFavoriteRecipeRepository favoriteRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(
            overviewReadService,
            new RecentRecipeReadService(recentRepository, overviewReadService),
            new FavoriteRecipeReadService(favoriteRepository),
            currentUserAccessService);

    private static GetRecentRecipesQueryHandler CreateRecentRecipesHandler(
        StubRecentItemRepository recentRepository,
        IRecipeOverviewReadService overviewReadService) =>
        new(new RecentRecipeReadService(recentRepository, overviewReadService), Substitute.For<ICurrentUserAccessService>());

    private static CreateRecipeCommand CreateRecipeCommand(
        Guid? userId,
        string visibility = "Private",
        Guid? imageAssetId = null,
        bool calculateNutritionAutomatically = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        double? manualAlcohol = null,
        IReadOnlyList<RecipeStepInput>? steps = null) {
        return new CreateRecipeCommand(
            userId,
            Name: "Soup",
            Description: null,
            Comment: null,
            Category: null,
            ImageUrl: null,
            ImageAssetId: imageAssetId,
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            Visibility: visibility,
            CalculateNutritionAutomatically: calculateNutritionAutomatically,
            ManualCalories: manualCalories,
            ManualProteins: manualProteins,
            ManualFats: manualFats,
            ManualCarbs: manualCarbs,
            ManualFiber: manualFiber,
            ManualAlcohol: manualAlcohol,
            Steps: steps ?? []);
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

    private static RecipeStepInput CreateRecipeStepWithProduct(int order, string description, Guid productId) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: productId, NestedRecipeId: null, Amount: 100)]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingNonNullImageAssetAccessService(Error error) : IImageAssetAccessService {
        public Task<Result<ImageAsset?>> ResolveOptionalAsync(
            ImageAssetId? assetId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                assetId.HasValue
                    ? Result.Failure<ImageAsset?>(error)
                    : Result.Success<ImageAsset?>(value: null));
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleRecipeRepository(Recipe recipe) : IRecipeRepository {
        public bool DeleteCalled { get; private set; }
        public Recipe? LastAddedRecipe { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            LastAddedRecipe = recipe;
            return Task.FromResult(recipe);
        }

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FindById(id, userId));

        private Recipe? FindById(RecipeId id, UserId userId) {
            if (LastAddedRecipe is not null && LastAddedRecipe.Id == id && LastAddedRecipe.UserId == userId) {
                return LastAddedRecipe;
            }

            return id == recipe.Id && userId == recipe.UserId ? recipe : null;
        }

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetUsageCountAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(recipe.MealItems.Count + recipe.NestedRecipeUsages.Count);

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleRecipeRepositoryForCreate : IRecipeRepository {
        public Recipe? LastAddedRecipe { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            LastAddedRecipe = recipe;
            return Task.FromResult(recipe);
        }

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

        public Task<int> GetUsageCountAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

    }

    [ExcludeFromCodeCoverage]
    private sealed class OverviewRecipeReadService(
        IReadOnlyList<(Recipe Recipe, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>? recipesByIdWithUsage = null) : IRecipeOverviewReadService {
        private readonly IReadOnlyList<(Recipe Recipe, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)> _recipesByIdWithUsage = recipesByIdWithUsage ?? new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)>();

        public Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            RecipeQueryFilters filters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<RecipeOverviewReadItem>)[.. _pagedItems.Select(item => ToReadItem(item.Recipe, item.UsageCount, userId))], _pagedItems.Count));

        public Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            var idSet = ids.ToHashSet();
            var filtered = _recipesByIdWithUsage
                .Where(pair => idSet.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => ToReadItem(pair.Value.Recipe, pair.Value.UsageCount, userId));
            return Task.FromResult<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>>(filtered);
        }

        public Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetExplorePagedAsync(
            UserId currentUserId,
            int page,
            int limit,
            string? search,
            string? category,
            int? maxPrepTime,
            string sortBy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<RecipeOverviewReadItem>)[.. _pagedItems.Select(item => ToReadItem(item.Recipe, item.UsageCount, currentUserId))], _pagedItems.Count));

        private static RecipeOverviewReadItem ToReadItem(Recipe recipe, int usageCount, UserId userId) {
            RecipeModel model = recipe.ToModel(usageCount, recipe.UserId == userId);
            return new RecipeOverviewReadItem(
                recipe.Id,
                recipe.UserId,
                model.Name,
                model.Description,
                model.Comment,
                model.Category,
                model.ImageUrl,
                recipe.ImageAssetId,
                model.PrepTime,
                model.CookTime,
                model.Servings,
                model.TotalCalories,
                model.TotalProteins,
                model.TotalFats,
                model.TotalCarbs,
                model.TotalFiber,
                model.TotalAlcohol,
                model.IsNutritionAutoCalculated,
                model.ManualCalories,
                model.ManualProteins,
                model.ManualFats,
                model.ManualCarbs,
                model.ManualFiber,
                model.ManualAlcohol,
                recipe.Visibility,
                model.UsageCount,
                model.CreatedAt,
                model.IsOwnedByCurrentUser,
                model.QualityScore,
                model.QualityGrade,
                [.. model.Steps.Select(ToReadStep)]);
        }

        private static RecipeOverviewStepReadItem ToReadStep(RecipeStepModel step) =>
            new(
                step.Id,
                step.StepNumber,
                step.Title,
                step.Instruction,
                step.ImageUrl,
                step.ImageAssetId,
                [.. step.Ingredients.Select(ToReadIngredient)]);

        private static RecipeOverviewIngredientReadItem ToReadIngredient(RecipeIngredientModel ingredient) =>
            new(
                ingredient.Id,
                ingredient.Amount,
                ingredient.ProductId,
                ingredient.ProductName,
                ingredient.ProductBaseUnit,
                ingredient.ProductBaseAmount,
                ingredient.ProductCaloriesPerBase,
                ingredient.ProductProteinsPerBase,
                ingredient.ProductFatsPerBase,
                ingredient.ProductCarbsPerBase,
                ingredient.ProductFiberPerBase,
                ingredient.ProductAlcoholPerBase,
                ingredient.NestedRecipeId,
                ingredient.NestedRecipeName,
                ingredient.NestedRecipeServings,
                ingredient.NestedRecipeTotalCalories,
                ingredient.NestedRecipeTotalProteins,
                ingredient.NestedRecipeTotalFats,
                ingredient.NestedRecipeTotalCarbs,
                ingredient.NestedRecipeTotalFiber,
                ingredient.NestedRecipeTotalAlcohol);
    }

    [ExcludeFromCodeCoverage]
    private sealed class OverviewRecipeRepository(
        IReadOnlyList<(Recipe Recipe, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>? recipesByIdWithUsage = null) : IRecipeRepository {
        private readonly IReadOnlyList<(Recipe Recipe, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)> _recipesByIdWithUsage = recipesByIdWithUsage ?? new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)>();

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

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

        public Task<int> GetUsageCountAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_pagedItems.FirstOrDefault(item => item.Recipe.Id == id).UsageCount);

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class StubFavoriteRecipeRepository(IReadOnlyList<FavoriteRecipe> favorites) : IFavoriteRecipeRepository {
        private readonly IReadOnlyList<FavoriteRecipe> _favorites = favorites;

        public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteRecipe?> GetByIdAsync(FavoriteRecipeId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteRecipe?> GetByRecipeIdAsync(RecipeId recipeId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByRecipeIdAsync(RecipeId recipeId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(UserId userId, IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, FavoriteRecipe>>(_favorites.Where(f => recipeIds.Contains(f.RecipeId)).ToDictionary(f => f.RecipeId));
        public Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites);
        public Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteRecipeReadModel>>([.. _favorites.Select(ToReadModel)]);

        private static FavoriteRecipeReadModel ToReadModel(FavoriteRecipe favorite) =>
            new(
                favorite.Id.Value,
                favorite.RecipeId.Value,
                favorite.Name,
                favorite.CreatedAtUtc,
                favorite.Recipe.Name,
                favorite.Recipe.ImageUrl,
                favorite.Recipe.TotalCalories ?? favorite.Recipe.ManualCalories,
                favorite.Recipe.Servings,
                favorite.Recipe.PrepTime,
                favorite.Recipe.CookTime,
                favorite.Recipe.Steps.Sum(step => step.Ingredients.Count));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(Deleted: true)
                : new DeleteImageAssetResult(Deleted: false, errorCode));
        }

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
    private sealed class EmptyProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowAllRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(
                ids.Distinct().ToDictionary(id => id, id => CreateNestedRecipe(userId, id)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class EmptyRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(new Dictionary<RecipeId, Recipe>());
    }

    private static Product CreateProduct(UserId userId, ProductId productId) {
        var product = Product.Create(userId, "Ingredient", MeasurementUnit.G, 100, defaultPortionAmount: null, 100, 1, 1, 1, 1, 0);
        typeof(Product)
            .GetProperty(nameof(Product.Id))!
            .SetValue(product, productId);
        return product;
    }

    private static Recipe CreateNestedRecipe(UserId userId, RecipeId recipeId) {
        var recipe = Recipe.Create(userId, "Nested", servings: 1);
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
        return recipe;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRecipeNutritionRepository : IRecipeRepository {
        public int UpdateNutritionCallCount { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUsageCountAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            UpdateNutritionCallCount++;
            return Task.CompletedTask;
        }

    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Error? error = user switch {
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error);
        }
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
