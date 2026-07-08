using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public partial class UpdateRecipeCommandHandlerTests {
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

    private static UpdateRecipeCommand UpdateCommand(
        Guid? userId,
        Guid recipeId,
        string? visibility = "Public",
        Guid? imageAssetId = null,
        bool calculateNutritionAutomatically = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        double? manualAlcohol = null,
        IReadOnlyList<RecipeStepInput>? steps = null) {
        return new UpdateRecipeCommand(
            userId,
            recipeId,
            Name: "Soup",
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            Category: null,
            ClearCategory: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: imageAssetId,
            ClearImageAssetId: false,
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
            Steps: steps ?? [CreateStep(order: 1, "Initial step")]);
    }

    private static void SetRecipeId(Recipe recipe, RecipeId recipeId) {
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
    }

    private static IRecipeRepository CreateRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe) {
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        repository
            .GetByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                RecipeId id = call.ArgAt<RecipeId>(0);
                UserId requestedUserId = call.ArgAt<UserId>(1);
                return Task.FromResult(id == recipeId && requestedUserId == userId ? recipe : null);
            });
        repository
            .UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        repository
            .UpdateNutritionAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return repository;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReloadMissingRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe) : IRecipeRepository {
        public bool UpdateCalled { get; private set; }

        public Task<Recipe> AddAsync(Recipe addedRecipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId ownerId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) {
            if (!UpdateCalled && id == recipeId && ownerId == userId) {
                return Task.FromResult<Recipe?>(recipe);
            }

            return Task.FromResult<Recipe?>(null);
        }

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId ownerId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetUsageCountAsync(
            RecipeId id,
            UserId ownerId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task UpdateAsync(Recipe updatedRecipe, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Recipe deletedRecipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe updatedRecipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

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
    private sealed class NoopImageAssetCleanupService : IImageAssetCleanupService {
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(Deleted: true));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageAssetCleanupService : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(new DeleteImageAssetResult(Deleted: true));
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
    private sealed class AllowAllRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(
                ids.Distinct().ToDictionary(id => id, id => CreateRecipe(userId, id)));
    }

    private static Product CreateProduct(UserId userId, ProductId productId) {
        var product = Product.Create(userId, "Ingredient", MeasurementUnit.G, 100, defaultPortionAmount: null, 100, 1, 1, 1, 1, 0);
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

    private static ICurrentUserAccessService CreateUserRepository(User user) {
        ICurrentUserAccessService repository = Substitute.For<ICurrentUserAccessService>();
        Error? error = user switch {
            { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
            { IsActive: false } => Errors.Authentication.InvalidToken,
            _ => null,
        };
        repository
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(error));

        return repository;
    }
}
