using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FoodDiary.Modules.Products.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class CatalogIdReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CatalogIdentityProjection_DistinguishesPublicPrivateAndMissingAndRejoinsTheLiveCallerTransaction() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("catalog-id-owner@example.test", "hash");
        var published = Product.Create(user.Id, "Published", MeasurementUnit.G, 100, 25, 100, 10, 5, 20, 3, 0, visibility: Visibility.Public);
        var privateProduct = Product.Create(user.Id, "Private", MeasurementUnit.G, 100, 25, 100, 10, 5, 20, 3, 0, visibility: Visibility.Private);
        database.Users.Add(user);
        database.Products.AddRange(published, privateProduct);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        IModuleTransactionCoordinator coordinator = Substitute.For<IModuleTransactionCoordinator>();
        coordinator.CurrentTransaction.Returns(_ => database.Database.CurrentTransaction?.GetDbTransaction());
        var services = new ServiceCollection();
        services.AddSingleton<IModuleContextFactory>(database);
        services.AddSingleton(coordinator);
        services.AddProductsPersistence();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IProductCatalogIdReadService reader = provider.GetRequiredService<IProductCatalogIdReadService>();
        ProductsDbContext owned = provider.GetRequiredService<ProductsDbContext>();
        using var cancellation = new CancellationTokenSource();

        Assert.True(await reader.CatalogIdIsPublicAsync(published.Id.Value, cancellation.Token));
        Assert.False(await reader.CatalogIdIsPublicAsync(privateProduct.Id.Value, cancellation.Token));
        Assert.Null(await reader.CatalogIdIsPublicAsync(Guid.NewGuid(), cancellation.Token));
        Assert.Empty(owned.ChangeTracker.Entries());
        await using (IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(cancellation.Token)) {
            Product changing = await owned.Products.SingleAsync(item => item.Id == privateProduct.Id, cancellation.Token);
            changing.ChangeVisibility(Visibility.Public);
            await owned.SaveChangesAsync(cancellation.Token);
            owned.ChangeTracker.Clear();

            Assert.True(await reader.CatalogIdIsPublicAsync(privateProduct.Id.Value, cancellation.Token));
            Assert.Empty(owned.ChangeTracker.Entries());
            await transaction.RollbackAsync(cancellation.Token);
        }

        Assert.False(await reader.CatalogIdIsPublicAsync(privateProduct.Id.Value, cancellation.Token));
        Assert.Empty(owned.ChangeTracker.Entries());
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.CatalogIdIsPublicAsync(published.Id.Value, cancellation.Token));
    }
}
