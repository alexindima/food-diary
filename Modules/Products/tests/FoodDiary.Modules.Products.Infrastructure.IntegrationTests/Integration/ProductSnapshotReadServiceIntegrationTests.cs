using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Products;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ProductSnapshotReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetByIdsAsync_ReturnsDistinctScalarSnapshotsWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"snapshots-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(user.Id, "Snapshot product", MeasurementUnit.G, 100, 25, 100, 10, 5, 20, 3, 0);
        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new ProductSnapshotReadService(context);

        IReadOnlyDictionary<ProductId, ProductSnapshotReadModel> result = await reader.GetByIdsAsync(
            [product.Id, product.Id, ProductId.New()]);

        ProductSnapshotReadModel snapshot = Assert.Single(result).Value;
        Assert.Multiple(
            () => Assert.Equal(product.Id, snapshot.Id),
            () => Assert.Equal(product.Name, snapshot.Name),
            () => Assert.Equal(product.BaseAmount, snapshot.BaseAmount),
            () => Assert.Equal(product.CaloriesPerBase, snapshot.CaloriesPerBase),
            () => Assert.Equal(product.Visibility, snapshot.Visibility),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetByIdsAsync_EmptyInputReturnsEmptyAndObservesCancellation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var reader = new ProductSnapshotReadService(context);
        Assert.Empty(await reader.GetByIdsAsync([]));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetByIdsAsync([], cancellation.Token));
    }
}
