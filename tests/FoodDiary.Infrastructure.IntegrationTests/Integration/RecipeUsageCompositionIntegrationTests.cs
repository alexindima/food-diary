using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using System.Data;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipeUsageCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task UsageQuerySharesMutationTransactionAndPreservesVisibilityAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create("usage-owner@example.com", "hash");
        var reader = User.Create("usage-reader@example.com", "hash");
        var recipe = Recipe.Create(owner.Id, "Usage recipe", servings: 1);
        recipe.ChangeVisibility(Visibility.Private);
        context.AddRange(owner, reader, recipe);
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
        services.AddRecipesPersistence();
        services.AddReadModelComposition();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IRecipeReadRepository reads = provider.GetRequiredService<IRecipeReadRepository>();
        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)) {
            Recipe? locked = await reads.GetByIdForUpdateAsync(recipe.Id, owner.Id, includePublic: false);
            Assert.NotNull(locked);
            var meal = Meal.Create(owner.Id, DateTime.UtcNow);
            meal.AddRecipe(recipe.Id, 1);
            var parent = Recipe.Create(owner.Id, "Parent recipe", servings: 1);
            parent.AddStep(1, "Mix").AddNestedRecipeIngredient(recipe.Id, 1);
            context.AddRange(meal, parent);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(2, await reads.GetUsageCountAsync(recipe.Id, owner.Id, includePublic: false));
            Assert.Equal(0, await reads.GetUsageCountAsync(recipe.Id, reader.Id));
            Assert.Equal(0, await reads.GetUsageCountAsync(RecipeId.New(), owner.Id));
            Assert.Empty(context.ChangeTracker.Entries());
            Recipe persisted = await context.Recipes.SingleAsync(candidate => candidate.Id == recipe.Id);
            persisted.ChangeVisibility(Visibility.Public);
            await context.SaveChangesAsync();
            Assert.Equal(2, await reads.GetUsageCountAsync(recipe.Id, reader.Id, includePublic: true));
            Assert.Equal(0, await reads.GetUsageCountAsync(recipe.Id, reader.Id, includePublic: false));
            await transaction.RollbackAsync();
        }
        context.ChangeTracker.Clear();
        Assert.Equal(0, await reads.GetUsageCountAsync(recipe.Id, owner.Id));
        Assert.Empty(await context.Meals.ToListAsync());
        Assert.Single(await context.Recipes.ToListAsync());
    }
}
