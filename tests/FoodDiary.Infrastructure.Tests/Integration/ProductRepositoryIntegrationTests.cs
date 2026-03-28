using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence.Products;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class ProductRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetPagedAsync_EscapesLikePatternAndReturnsExactProductMatch() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userId = user.Id;
        var matchingProduct = Product.Create(
            userId,
            "100% Cocoa",
            MeasurementUnit.G,
            100,
            25,
            100,
            10,
            5,
            20,
            3,
            0);
        var otherProduct = Product.Create(
            userId,
            "1000 Cocoa",
            MeasurementUnit.G,
            100,
            25,
            100,
            10,
            5,
            20,
            3,
            0);
        context.Products.AddRange(matchingProduct, otherProduct);
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            userId,
            includePublic: false,
            page: 0,
            limit: 0,
            search: "100% Cocoa");

        var item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingProduct.Id, item.Product.Id);
    }
}
