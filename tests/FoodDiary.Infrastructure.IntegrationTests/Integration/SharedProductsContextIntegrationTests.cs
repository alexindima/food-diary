using System.Data;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedProductsContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSaveAndIntermediateMutationFlushUseOwnerContextAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        ProductsDbContext owned = provider.GetRequiredService<ProductsDbContext>();
        IProductWriteRepository writes = provider.GetRequiredService<IProductWriteRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create("product-owner@example.com", "hash");
        Product product = CreateProduct(user);
        shared.Users.Add(user);
        await writes.AddAsync(product);
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await unitOfWork.SaveChangesAsync();
        Assert.Single(owned.Model.GetEntityTypes());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(shared.ChangeTracker.Entries<Product>());
        Assert.Single(await database.Products.ToListAsync());
        uint createdVersion = owned.Entry(product).Property<uint>("xmin").CurrentValue;
        Assert.NotEqual(0u, createdVersion);
        await provider.GetRequiredService<IProductMutationTransactionRunner>().ExecuteAsync(async token => {
            Product? locked = await writes.GetByIdForUpdateAsync(product.Id, user.Id, includePublic: false, token);
            Assert.NotNull(locked);
            Assert.Equal(IsolationLevel.Serializable, shared.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            Assert.Same(shared.Database.CurrentTransaction!.GetDbTransaction(), owned.Database.CurrentTransaction!.GetDbTransaction());
            locked.UpdateCoreIdentity(name: "Updated owner product");
            await writes.UpdateAsync(locked, token);
            await unitOfWork.SaveChangesAsync(token);
            Assert.NotEqual(createdVersion, owned.Entry(locked).Property<uint>("xmin").CurrentValue);
            IReadOnlyDictionary<ProductId, ProductSnapshotReadModel> snapshots = await provider.GetRequiredService<IProductSnapshotReadService>().GetByIdsAsync([product.Id], token);
            Assert.Equal("Updated owner product", snapshots[product.Id].Name);
            Assert.NotNull(await writes.GetByIdForUpdateAsync(product.Id, user.Id, includePublic: false, token));
            Assert.Equal(0, await provider.GetRequiredService<IProductReadRepository>().GetUsageCountAsync(product.Id, user.Id, includePublic: false, token));
            return Result.Success();
        });
        Assert.Equal("Updated owner product", (await database.Products.AsNoTracking().SingleAsync()).Name);
    }

    [RequiresDockerFact]
    public async Task FailureAfterFlushRollsBackBothContextsAndAllowsAnotherAttemptAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        (User user, Product product) = await SeedAsync(provider);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        ProductsDbContext owned = provider.GetRequiredService<ProductsDbContext>();
        IProductWriteRepository writes = provider.GetRequiredService<IProductWriteRepository>();
        IProductMutationTransactionRunner runner = provider.GetRequiredService<IProductMutationTransactionRunner>();
        Result result = await runner.ExecuteAsync(async token => {
            Product? locked = await writes.GetByIdForUpdateAsync(product.Id, user.Id, includePublic: false, token);
            Assert.NotNull(locked);
            locked.UpdateCoreIdentity(name: "Must roll back");
            shared.Users.Add(User.Create("rolled-back-product-user@example.com", "hash"));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);
            return Result.Failure(new Error("Products.TestFailure", "Simulated failure after flush", ErrorKind.Conflict));
        });
        Assert.True(result.IsFailure);
        Assert.Empty(shared.ChangeTracker.Entries());
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.Single(await database.Users.ToListAsync());
        Assert.Equal("Owner product", (await database.Products.AsNoTracking().SingleAsync()).Name);
        await runner.ExecuteAsync(async token => {
            Product? locked = await writes.GetByIdForUpdateAsync(product.Id, user.Id, includePublic: false, token);
            Assert.NotNull(locked);
            locked.UpdateCoreIdentity(name: "Next attempt");
            return Result.Success();
        });
        Assert.Equal("Next attempt", (await database.Products.AsNoTracking().SingleAsync()).Name);
    }

    [RequiresDockerFact]
    public async Task OwnerRowLockBlocksIndependentMutationAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        string connectionString = database.Database.GetConnectionString()!;
        await using ServiceProvider first = CreateProvider(connectionString);
        await using ServiceProvider second = CreateProvider(connectionString, enableRetries: false);
        (User user, Product product) = await SeedAsync(first);
        await first.GetRequiredService<IProductMutationTransactionRunner>().ExecuteAsync(async token => {
            Assert.NotNull(await first.GetRequiredService<IProductWriteRepository>().GetByIdForUpdateAsync(product.Id, user.Id, includePublic: false, token));
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                second.GetRequiredService<IProductMutationTransactionRunner>().ExecuteAsync(async secondToken => {
                    await second.GetRequiredService<FoodDiaryDbContext>().Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'", secondToken);
                    return await second.GetRequiredService<IProductWriteRepository>().GetByIdForUpdateAsync(product.Id, user.Id, includePublic: false, secondToken);
                }));
            PostgresException exception = Assert.IsType<PostgresException>(failure.InnerException);
            Assert.Equal(PostgresErrorCodes.LockNotAvailable, exception.SqlState);
            return Result.Success();
        });
        Assert.Empty(second.GetRequiredService<ProductsDbContext>().ChangeTracker.Entries());
    }

    private static Product CreateProduct(User user) => Product.Create(user.Id, "Owner product", MeasurementUnit.G, 100, 100, 100, 10, 5, 10, 1, 0);

    private static async Task<(User User, Product Product)> SeedAsync(ServiceProvider provider) {
        var user = User.Create("product-owner@example.com", "hash");
        Product product = CreateProduct(user);
        provider.GetRequiredService<FoodDiaryDbContext>().Users.Add(user);
        await provider.GetRequiredService<IProductWriteRepository>().AddAsync(product);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return (user, product);
    }

    private static ServiceProvider CreateProvider(string connectionString, bool enableRetries = true) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = enableRetries.ToString(),
            ["Database:MaxRetryDelaySeconds"] = "1",
        }).Build());
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddProductsPersistence();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
