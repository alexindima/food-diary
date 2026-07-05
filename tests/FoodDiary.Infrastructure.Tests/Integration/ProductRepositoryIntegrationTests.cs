using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Products.Models;
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

        var readService = new ProductOverviewReadService(context);

        (IReadOnlyList<ProductOverviewReadItem>? items, int totalItems) = await readService.GetPagedAsync(
            userId,
            includePublic: false,
            page: 0,
            limit: 0,
            filters: new ProductQueryFilters("100% Cocoa"));

        ProductOverviewReadItem item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingProduct.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesStructuredFilters() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-filters-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        (Product dairyWithImage, Product grainNoImage, Product lowCalorieDairy) =
            await SeedProductFiltersAsync(context, user.Id);
        var readService = new ProductOverviewReadService(context);

        IReadOnlyList<ProductId> dairyIds = await GetProductIdsAsync(readService, user.Id, new ProductQueryFilters(
            Search: null,
            ProductTypes: [ProductType.Dairy]));
        IReadOnlyList<ProductId> calorieIds = await GetProductIdsAsync(readService, user.Id, new ProductQueryFilters(
            Search: null,
            CaloriesFrom: 100,
            CaloriesTo: 150));
        IReadOnlyList<ProductId> withImageIds = await GetProductIdsAsync(readService, user.Id, new ProductQueryFilters(
            Search: null,
            HasImage: true));
        IReadOnlyList<ProductId> withoutImageIds = await GetProductIdsAsync(readService, user.Id, new ProductQueryFilters(
            Search: null,
            HasImage: false));

        AssertIds([dairyWithImage.Id, lowCalorieDairy.Id], dairyIds);
        Assert.Equal([dairyWithImage.Id], calorieIds);
        Assert.Equal([dairyWithImage.Id], withImageIds);
        AssertIds([grainNoImage.Id, lowCalorieDairy.Id], withoutImageIds);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_FirstOwnedPage_StaysWithinLatencyBudget() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-perf-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Product[] products = [.. Enumerable.Range(0, PerformanceSeedCount)
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
                0))];

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        var readService = new ProductOverviewReadService(context);

        _ = await readService.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            filters: new ProductQueryFilters(Search: null));

        var stopwatch = Stopwatch.StartNew();
        (IReadOnlyList<ProductOverviewReadItem>? items, int totalItems) = await readService.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            filters: new ProductQueryFilters(Search: null));
        stopwatch.Stop();

        Assert.Equal(PerformanceSeedCount, totalItems);
        Assert.Equal(25, items.Count);
        Assert.True(
            stopwatch.Elapsed <= FirstPageLatencyBudget,
            string.Create(CultureInfo.InvariantCulture, $"Expected ProductOverviewReadService.GetPagedAsync first page to stay within {FirstPageLatencyBudget.TotalMilliseconds} ms on seeded PostgreSQL data, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms."));
    }

    private static async Task<IReadOnlyList<ProductId>> GetProductIdsAsync(
        ProductOverviewReadService readService,
        UserId userId,
        ProductQueryFilters filters) {
        (IReadOnlyList<ProductOverviewReadItem> items, int _) = await readService.GetPagedAsync(
            userId,
            includePublic: false,
            page: 1,
            limit: 50,
            filters: filters).ConfigureAwait(false);

        return [.. items.Select(item => item.Id)];
    }

    private static async Task<(Product DairyWithImage, Product GrainNoImage, Product LowCalorieDairy)> SeedProductFiltersAsync(
        FoodDiaryDbContext context,
        UserId userId) {
        var dairyWithImage = Product.Create(
            userId,
            "Greek yogurt",
            MeasurementUnit.G,
            100,
            150,
            120,
            8,
            4,
            12,
            0,
            0,
            productType: ProductType.Dairy,
            imageUrl: "https://cdn.example.com/yogurt.webp");
        var grainNoImage = Product.Create(userId, "Buckwheat", MeasurementUnit.G, 100, 70, 340, 12, 3, 71, 10, 0, productType: ProductType.Grain);
        var lowCalorieDairy = Product.Create(userId, "Skim milk", MeasurementUnit.Ml, 100, 200, 42, 3, 0, 5, 0, 0, productType: ProductType.Dairy);

        context.Products.AddRange(dairyWithImage, grainNoImage, lowCalorieDairy);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return (dairyWithImage, grainNoImage, lowCalorieDairy);
    }

    private static void AssertIds(IReadOnlyCollection<ProductId> expected, IReadOnlyCollection<ProductId> actual) =>
        Assert.Equal(
            [.. expected.Select(id => id.Value).Order()],
            [.. actual.Select(id => id.Value).Order()]);
}
