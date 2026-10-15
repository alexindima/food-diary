using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Infrastructure;
using FoodDiary.Modules.Recipes.Infrastructure;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using System.Data;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
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
        IRecipeOverviewReadService overview = provider.GetRequiredService<IRecipeOverviewReadService>();
        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)) {
            Recipe? locked = await reads.GetByIdForUpdateAsync(recipe.Id, owner.Id, includePublic: false);
            Assert.NotNull(locked);
            var meal = Meal.Create(owner.Id, DateTime.UtcNow);
            meal.AddRecipe(recipe.Id, RecipeServingQuantity.FromServings(1));
            var parent = Recipe.Create(owner.Id, "Parent recipe", servings: 1);
            parent.AddStep(1, "Mix").AddNestedRecipeIngredient(recipe.Id, RecipeServingQuantity.FromServings(1));
            var catalogPlan = MealPlan.CreateCurated("Catalog plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
            catalogPlan.AddDay(1).AddMeal(MealType.Breakfast, recipe.Id);
            var hiddenPlan = MealPlan.CreateCurated("Hidden plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
            hiddenPlan.UpdateCatalogDetails("Hidden plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null, isPublished: false);
            hiddenPlan.AddDay(1).AddMeal(MealType.Lunch, recipe.Id);
            var adoptedPlan = MealPlan.CreateForUser(reader.Id, "Another user's plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
            adoptedPlan.AddDay(1).AddMeal(MealType.Dinner, recipe.Id);
            context.AddRange(meal, parent, catalogPlan, hiddenPlan, adoptedPlan);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(5, await reads.GetUsageCountAsync(recipe.Id, owner.Id, includePublic: false));
            Assert.Equal(5, (await overview.GetByIdsWithUsageAsync([recipe.Id], owner.Id))[recipe.Id].UsageCount);
            (IReadOnlyList<RecipeOverviewReadItem> items, _) = await overview.GetPagedAsync(
                owner.Id, includePublic: false, 1, 10, new RecipeQueryFilters("Usage recipe"));
            Assert.Equal(5, Assert.Single(items).UsageCount);
            Assert.Equal(0, await reads.GetUsageCountAsync(recipe.Id, reader.Id));
            Assert.Equal(0, await reads.GetUsageCountAsync(RecipeId.New(), owner.Id));
            Assert.Empty(context.ChangeTracker.Entries());
            Recipe persisted = await context.Recipes.SingleAsync(candidate => candidate.Id == recipe.Id);
            persisted.ChangeVisibility(Visibility.Public);
            await context.SaveChangesAsync();
            Assert.Equal(5, await reads.GetUsageCountAsync(recipe.Id, reader.Id, includePublic: true));
            Assert.Equal(0, await reads.GetUsageCountAsync(recipe.Id, reader.Id, includePublic: false));
            (IReadOnlyList<RecipeOverviewReadItem> popularItems, _) = await overview.GetExplorePagedAsync(
                reader.Id, 1, 10, search: null, category: null, maxPrepTime: null, "popular");
            Assert.Equal(5, Assert.Single(popularItems).UsageCount);
            await transaction.RollbackAsync();
        }
        context.ChangeTracker.Clear();
        Assert.Equal(0, await reads.GetUsageCountAsync(recipe.Id, owner.Id));
        Assert.Empty(await context.Meals.ToListAsync());
        Assert.Empty(await context.MealPlans.ToListAsync());
        Assert.Single(await context.Recipes.ToListAsync());
    }
}
