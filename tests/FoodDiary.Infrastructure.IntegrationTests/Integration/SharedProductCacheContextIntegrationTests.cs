using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Domain.Entities.OpenFoodFacts;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedProductCacheContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyOwnedCacheTable() {
        using var context = new OpenFoodFactsDbContext(new DbContextOptionsBuilder<OpenFoodFactsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.Equal(typeof(OpenFoodFactsProduct), Assert.Single(context.Model.GetEntityTypes()).ClrType);
        Assert.Equal("OpenFoodFactsProducts", context.Model.FindEntityType(typeof(OpenFoodFactsProduct))!.GetTableName());
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CacheFollowsTransactionOpenedAfterResolutionAndResetsAfterCompletionAsync(bool commit) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(central);
        services.AddSingleton<SharedPersistenceDbContext>(central);
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddOpenFoodFactsModule();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IOpenFoodFactsProductCacheRepository repository = provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>();
        OpenFoodFactsDbContext owned = provider.GetRequiredService<OpenFoodFactsDbContext>();
        Assert.Same(central.Database.GetDbConnection(), owned.Database.GetDbConnection());
        var product = new OpenFoodFactsProductModel("tx-cache", "Transactional cache item", Brand: null,
            Category: null, ImageUrl: null, CaloriesPer100G: null, ProteinsPer100G: null,
            FatsPer100G: null, CarbsPer100G: null, FiberPer100G: null);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            await repository.UpsertAsync([product]);
            Assert.Single(await repository.SearchAsync(product.Name));
            Assert.False(await read.OpenFoodFactsProducts.AnyAsync(item => item.Barcode == product.Barcode));
            if (commit) {
                await transaction.CommitAsync();
            } else {
                await transaction.RollbackAsync();
            }
        }
        Assert.Equal(commit, await read.OpenFoodFactsProducts.AnyAsync(item => item.Barcode == product.Barcode));
        await repository.UpsertAsync([product with { Barcode = "after-tx", Name = "After transaction" }]);
        Assert.Single(await repository.SearchAsync("After transaction"));
        Assert.True(await read.OpenFoodFactsProducts.AnyAsync(item => item.Barcode == "after-tx"));
        Assert.Empty(owned.ChangeTracker.Entries());
    }
}
