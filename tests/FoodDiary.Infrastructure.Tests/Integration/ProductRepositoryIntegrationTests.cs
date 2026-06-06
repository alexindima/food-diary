using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Products;
using System.Diagnostics;
using System.Globalization;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ProductRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int PerformanceSeedCount = 1500;
    private static readonly TimeSpan FirstPageLatencyBudget = TimeSpan.FromMilliseconds(250);

    [RequiresDockerFact]
    public async Task GetPagedAsync_EscapesLikePatternAndReturnsExactProductMatch() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        UserId userId = user.Id;
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

        (IReadOnlyList<(Product Product, int UsageCount)>? items, int totalItems) = await repository.GetPagedAsync(
            userId,
            includePublic: false,
            page: 0,
            limit: 0,
            search: "100% Cocoa");

        (Product Product, int UsageCount) item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingProduct.Id, item.Product.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_FirstOwnedPage_StaysWithinLatencyBudget() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-perf-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Product[] products = Enumerable.Range(0, PerformanceSeedCount)
            .Select(index => Product.Create(
                user.Id,
                string.Create(CultureInfo.InvariantCulture, $"Perf Product {index:D4}"),
                MeasurementUnit.G,
                100,
                25,
                100,
                10,
                5,
                20,
                3,
                0))
            .ToArray();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        _ = await repository.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            search: null);

        var stopwatch = Stopwatch.StartNew();
        (IReadOnlyList<(Product Product, int UsageCount)>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            search: null);
        stopwatch.Stop();

        Assert.Equal(PerformanceSeedCount, totalItems);
        Assert.Equal(25, items.Count);
        Assert.True(
            stopwatch.Elapsed <= FirstPageLatencyBudget,
            string.Create(CultureInfo.InvariantCulture, $"Expected ProductRepository.GetPagedAsync first page to stay within {FirstPageLatencyBudget.TotalMilliseconds} ms on seeded PostgreSQL data, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms."));
    }
}
