using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Favorites;

[ExcludeFromCodeCoverage]
public sealed class FavoriteWriteContractTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProductOwnedRead_ForwardsArgumentsAndOriginalTask(bool asTracking) {
        var userId = UserId.New();
        var favorite = FavoriteProduct.Create(userId, ProductId.New());
        using var cancellation = new CancellationTokenSource();
        Task<FavoriteProduct?> expected = Task.FromResult<FavoriteProduct?>(favorite);
        int calls = 0;
        IFavoriteProductWriteRepository repository = new ProductRepository((id, owner, tracking, token) => {
            calls++;
            Assert.Multiple(
                () => Assert.Equal(favorite.Id, id),
                () => Assert.Equal(userId, owner),
                () => Assert.Equal(asTracking, tracking),
                () => Assert.Equal(cancellation.Token, token));
            return expected;
        });

        Task<FavoriteProduct?> actual = repository.GetOwnedByIdAsync(favorite.Id, userId, asTracking, cancellation.Token);

        Assert.Same(expected, actual);
        Assert.Same(favorite, await actual);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ProductOwnedRead_PreservesOptionalDefaultsAndMissingResult() {
        var userId = UserId.New();
        var favoriteId = FavoriteProductId.New();
        IFavoriteProductWriteRepository repository = new ProductRepository((id, owner, tracking, token) => {
            Assert.Multiple(
                () => Assert.Equal(favoriteId, id),
                () => Assert.Equal(userId, owner),
                () => Assert.False(tracking),
                () => Assert.Equal(CancellationToken.None, token));
            return Task.FromResult<FavoriteProduct?>(null);
        });

        Assert.Null(await repository.GetOwnedByIdAsync(favoriteId, userId));
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProductRepository(
        Func<FavoriteProductId, UserId, bool, CancellationToken, Task<FavoriteProduct?>> read)
        : IFavoriteProductWriteRepository {
        public Task<FavoriteProduct?> GetByIdAsync(FavoriteProductId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) =>
            read(id, userId, asTracking, cancellationToken);

        public Task<FavoriteProduct?> GetByProductIdAsync(ProductId productId, UserId userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecipeOwnedRead_ForwardsArgumentsAndOriginalTask(bool asTracking) {
        var userId = UserId.New();
        var favorite = FavoriteRecipe.Create(userId, RecipeId.New());
        using var cancellation = new CancellationTokenSource();
        Task<FavoriteRecipe?> expected = Task.FromResult<FavoriteRecipe?>(favorite);
        int calls = 0;
        IFavoriteRecipeWriteRepository repository = new RecipeRepository((id, owner, tracking, token) => {
            calls++;
            Assert.Multiple(
                () => Assert.Equal(favorite.Id, id),
                () => Assert.Equal(userId, owner),
                () => Assert.Equal(asTracking, tracking),
                () => Assert.Equal(cancellation.Token, token));
            return expected;
        });

        Task<FavoriteRecipe?> actual = repository.GetOwnedByIdAsync(favorite.Id, userId, asTracking, cancellation.Token);

        Assert.Same(expected, actual);
        Assert.Same(favorite, await actual);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RecipeOwnedRead_PreservesOptionalDefaultsAndMissingResult() {
        var userId = UserId.New();
        var favoriteId = FavoriteRecipeId.New();
        IFavoriteRecipeWriteRepository repository = new RecipeRepository((id, owner, tracking, token) => {
            Assert.Multiple(
                () => Assert.Equal(favoriteId, id),
                () => Assert.Equal(userId, owner),
                () => Assert.False(tracking),
                () => Assert.Equal(CancellationToken.None, token));
            return Task.FromResult<FavoriteRecipe?>(null);
        });

        Assert.Null(await repository.GetOwnedByIdAsync(favoriteId, userId));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecipeRepository(
        Func<FavoriteRecipeId, UserId, bool, CancellationToken, Task<FavoriteRecipe?>> read)
        : IFavoriteRecipeWriteRepository {
        public Task<FavoriteRecipe?> GetByIdAsync(FavoriteRecipeId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) =>
            read(id, userId, asTracking, cancellationToken);

        public Task<FavoriteRecipe?> GetByRecipeIdAsync(RecipeId recipeId, UserId userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
