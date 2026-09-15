using FoodDiary.Modules.Recipes.Infrastructure.Persistence.Recipes;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Infrastructure;
using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition.Recipes;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Modules.Products.Infrastructure.Persistence.Products;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ModuleBoundaryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task RecipeReadForUpdate_LoadsProductSnapshotWithoutTrackingForeignAggregates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recipe-boundary@example.com", "hash");
        var product = Product.Create(user.Id, "Apple", MeasurementUnit.G, 100, 100, 52, 1, 1, 11, 2, 0);
        var recipe = Recipe.Create(user.Id, "Apple dish", 1);
        recipe.AddStep(1, "Prepare").AddProductIngredient(product.Id, 100);
        context.Users.Add(user);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await using var owned = new RecipesDbContext(new DbContextOptionsBuilder<RecipesDbContext>()
            .UseNpgsql(context.Database.GetDbConnection()).Options);
        var repository = new RecipeRepository(owned, new ProductSnapshotReadService(context.Products), new RecipeUsageQuery(context));
        Recipe loaded = Assert.IsType<Recipe>(await repository.GetByIdForUpdateAsync(recipe.Id, user.Id, includeSteps: true));
        RecipeIngredientProductSnapshot snapshot = Assert.IsType<RecipeIngredientProductSnapshot>(Assert.Single(Assert.Single(loaded.Steps).Ingredients).ProductSnapshot);
        Assert.Equal("Apple", snapshot.Name);
        Assert.Equal(52, snapshot.CaloriesPerBase);
        Assert.Empty(context.ChangeTracker.Entries<Product>());
        Assert.Empty(context.ChangeTracker.Entries<User>());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task MealDailyCalories_PreservesDateBoundsRoundingAndUserIsolation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("calories-owner@example.com", "hash");
        var other = User.Create("calories-other@example.com", "hash");
        DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Users.AddRange(user, other);
        foreach ((User owner, DateTime date, double calories) in new[] {
            (user, start.AddHours(1), 100.125), (user, start.AddHours(2), 50.125),
            (user, start.AddDays(1), 200d), (other, start.AddHours(1), 999d),
            (user, start.AddTicks(-1), 500d), (user, start.AddDays(2), 30d),
        }) {
            var meal = Meal.Create(owner.Id, date);
            meal.ApplyNutrition(new MealNutritionUpdate(calories, 1, 1, 1, 0, 0, IsAutoCalculated: false, calories, 1, 1, 1, 0, 0));
            context.Meals.Add(meal);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(context);
        services.AddSingleton<IModuleTransactionCoordinator>(new EfModuleTransactionCoordinator(context,
            new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance)));
        services.AddMealsPersistence();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IMealDailyCalorieReadService reader = provider.GetRequiredService<IMealDailyCalorieReadService>();

        Result<IReadOnlyList<MealDailyCalories>> result = await reader.GetDailyCaloriesAsync(user.Id, start, start.AddDays(2));

        Assert.True(result.IsSuccess);
        // Meal.ApplyNutrition rounds each persisted meal before daily aggregation.
        Assert.Collection(result.Value,
            day => Assert.Equal(150.24, day.TotalCalories, precision: 2),
            day => Assert.Equal(200, day.TotalCalories),
            day => Assert.Equal(30, day.TotalCalories));
        Assert.Empty(context.ChangeTracker.Entries<Meal>());
    }

    [RequiresDockerFact]
    public async Task BillingTransaction_RejectsCallerChangesAndNestedTransactionsWithoutLosingCallerState() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("transaction-boundary@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        user.MarkDeleted(DateTime.UtcNow);
        var runner = new EfBillingTransactionRunner(new EfModuleTransactionCoordinator(context, new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance)));
        bool invoked = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync(_ => {
            invoked = true;
            return Task.CompletedTask;
        }));

        Assert.False(invoked);
        Assert.True(context.ChangeTracker.HasChanges());
        Assert.Null(await context.Users.AsNoTracking().Where(candidate => candidate.Id == user.Id).Select(candidate => candidate.DeletedAt).SingleAsync());
        context.ChangeTracker.Clear();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync(_ => Task.CompletedTask));
        await transaction.RollbackAsync();
    }
}
