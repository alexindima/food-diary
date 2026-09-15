using FoodDiary.Modules.Products.Infrastructure;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using System.Data;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ProductUsageCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task UsageQuerySharesMutationTransactionAndPreservesVisibilityAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create("usage-owner@example.com", "hash");
        var reader = User.Create("usage-reader@example.com", "hash");
        var product = Product.Create(owner.Id, "Usage product", MeasurementUnit.G, 100, 100, 100, 10, 5, 10, 1, 0,
            visibility: Visibility.Private);
        context.AddRange(owner, reader, product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(context);
        services.AddSingleton<IModuleTransactionCoordinator>(new EfModuleTransactionCoordinator(context,
            new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance)));
        services.AddMemoryCache();
        services.AddProductsPersistence();
        services.AddReadModelComposition();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IProductReadRepository reads = provider.GetRequiredService<IProductReadRepository>();
        IProductWriteRepository writes = provider.GetRequiredService<IProductWriteRepository>();
        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)) {
            Product? locked = await writes.GetByIdForUpdateAsync(product.Id, owner.Id, includePublic: false);
            Assert.NotNull(locked);
            var meal = Meal.Create(owner.Id, DateTime.UtcNow);
            meal.AddProduct(product.Id, 100);
            var recipe = Recipe.Create(owner.Id, "Usage recipe", servings: 1);
            recipe.AddStep(1, "Mix").AddProductIngredient(product.Id, 100);
            context.AddRange(meal, recipe);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(2, await reads.GetUsageCountAsync(product.Id, owner.Id, includePublic: false));
            Assert.Equal(0, await reads.GetUsageCountAsync(product.Id, reader.Id));
            Assert.Equal(0, await reads.GetUsageCountAsync(ProductId.New(), owner.Id));
            Assert.Empty(context.ChangeTracker.Entries());
            Product persisted = await context.Products.SingleAsync(candidate => candidate.Id == product.Id);
            persisted.ChangeVisibility(Visibility.Public);
            await context.SaveChangesAsync();
            Assert.Equal(2, await reads.GetUsageCountAsync(product.Id, reader.Id, includePublic: true));
            Assert.Equal(0, await reads.GetUsageCountAsync(product.Id, reader.Id, includePublic: false));
            await transaction.RollbackAsync();
        }
        context.ChangeTracker.Clear();
        Assert.Equal(0, await reads.GetUsageCountAsync(product.Id, owner.Id));
        Assert.Empty(await context.Meals.ToListAsync());
        Assert.Empty(await context.Recipes.ToListAsync());
    }
}
