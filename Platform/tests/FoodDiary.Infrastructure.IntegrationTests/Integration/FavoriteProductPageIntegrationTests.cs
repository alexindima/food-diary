using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.Favorites;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class FavoriteProductPageIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task PreservesOwnerVisibilityLiteralSearchAndPagingAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"product-page-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"product-other-{Guid.NewGuid():N}@example.com", "hash");
        var first = Product.Create(owner.Id, "Rice_100%", MeasurementUnit.G, 100, 100, 130, 2, 1, 28, 4, 0, imageUrl: "https://example.com/rice.jpg");
        var second = Product.Create(other.Id, "Public rice", MeasurementUnit.G, 100, 100, 130, 2, 1, 28, 4, 0, comment: "private note", visibility: Visibility.Public);
        var hidden = Product.Create(other.Id, "Hidden rice", MeasurementUnit.G, 100, 100, 130, 2, 1, 28, 4, 0, visibility: Visibility.Private);
        context.Users.AddRange(owner, other);
        context.Products.AddRange(first, second, hidden);
        context.FavoriteProducts.AddRange(FavoriteProduct.Create(owner.Id, first.Id), FavoriteProduct.Create(owner.Id, second.Id), FavoriteProduct.Create(owner.Id, hidden.Id), FavoriteProduct.Create(other.Id, first.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var query = new FavoriteProductQuery(context);
        Assert.Empty(await query.GetByProductIdsReadModelsAsync(owner.Id, []));
        (IReadOnlyList<FavoriteProductReadModel> one, int total) = await query.GetPageReadModelsAsync(owner.Id, 1, 1, search: null);
        (IReadOnlyList<FavoriteProductReadModel> two, _) = await query.GetPageReadModelsAsync(owner.Id, 2, 1, search: null);
        Assert.Equal(2, total);
        Assert.NotEqual(Assert.Single(one).Id, Assert.Single(two).Id);
        (IReadOnlyList<FavoriteProductReadModel> matches, int count) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, " rice_100% ");
        FavoriteProductReadModel match = Assert.Single(matches);
        Assert.Multiple(() => Assert.Equal(1, count), () => Assert.Equal(first.Id.Value, match.ProductId), () => Assert.Equal("https://example.com/rice.jpg", match.ImageUrl));
        (IReadOnlyList<FavoriteProductReadModel> preview, int countOnly) = await query.GetPageReadModelsAsync(owner.Id, 1, 0, search: null);
        Assert.Empty(preview);
        Assert.Equal(2, countOnly);
        IReadOnlyList<FavoriteProductReadModel> selected = await query.GetByProductIdsReadModelsAsync(owner.Id, [second.Id, hidden.Id]);
        Assert.Null(Assert.Single(selected).Comment);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
