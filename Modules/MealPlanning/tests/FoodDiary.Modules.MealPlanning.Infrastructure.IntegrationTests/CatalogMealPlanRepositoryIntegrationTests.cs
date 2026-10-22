using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.MealPlanning;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class CatalogMealPlanRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CatalogPages_FilterPersonalPlansAndUseStableTimeAndIdentityOrderingWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("catalog-page-owner@example.test", "hash");
        var first = MealPlan.CreateCurated("First catalog", "First description", DietType.Balanced, 2, 2000);
        var second = MealPlan.CreateCurated("Second catalog", description: null, DietType.Keto, 3, 1800);
        var older = MealPlan.CreateCurated("Older catalog", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        var personal = MealPlan.CreateForUser(user.Id, "Personal plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        context.AddRange(user, first, second, older, personal);
        await context.SaveChangesAsync();
        var createdAt = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        await context.MealPlans.ExecuteUpdateAsync(setters => setters.SetProperty(plan => plan.CreatedOnUtc, createdAt));
        await context.MealPlans.Where(plan => plan.Id == older.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(plan => plan.CreatedOnUtc, createdAt.AddDays(-1)));
        await context.MealPlans.Where(plan => plan.Id == personal.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(plan => plan.CreatedOnUtc, createdAt.AddDays(1)));
        context.ChangeTracker.Clear();
        var repository = new MealPlanRepository(context.MealPlans, new MealPlanCompositionReader(context));
        using var cancellation = new CancellationTokenSource();
        Guid[] tied = [.. new[] { first.Id.Value, second.Id.Value }.Order()];

        MealPlanSummaryReadModel pageOne = Assert.Single(await repository.GetPageAsync(1, 1, cancellation.Token));
        MealPlanSummaryReadModel pageTwo = Assert.Single(await repository.GetPageAsync(2, 1, cancellation.Token));
        MealPlanSummaryReadModel pageThree = Assert.Single(await repository.GetPageAsync(3, 1, cancellation.Token));
        MealPlanSummaryReadModel normalized = Assert.Single(await repository.GetPageAsync(0, 0, cancellation.Token));
        IReadOnlyList<MealPlanSummaryReadModel> all = await repository.GetPageAsync(1, int.MaxValue, cancellation.Token);

        Assert.Multiple(
            () => Assert.Equal(tied[0], pageOne.Id),
            () => Assert.Equal(tied[1], pageTwo.Id),
            () => Assert.Equal(older.Id.Value, pageThree.Id),
            () => Assert.Equal(pageOne, normalized),
            () => Assert.Equal(new[] { tied[0], tied[1], older.Id.Value }, all.Select(plan => plan.Id)),
            () => Assert.All(all, plan => Assert.True(plan.IsCurated)),
            () => Assert.DoesNotContain(all, plan => plan.Id == personal.Id.Value),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        Assert.Empty(await repository.GetPageAsync(int.MaxValue, int.MaxValue, cancellation.Token));
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetPageAsync(1, 1, cancellation.Token));
    }

    [RequiresDockerFact]
    public async Task CatalogUpdate_LoadsTheTrackedOwnedGraphAndPersistsOnlyTheExplicitCatalogMutation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("catalog-update-owner@example.test", "hash");
        var recipe = Recipe.Create(user.Id, "Catalog dish", 2);
        var catalog = MealPlan.CreateCurated("Catalog plan", description: null, DietType.Balanced, 2, targetCaloriesPerDay: null);
        MealPlanDay day = catalog.AddDay(1);
        MealPlanMeal meal = day.AddMeal(MealType.Lunch, recipe.Id, 3);
        var personal = MealPlan.CreateForUser(user.Id, "Personal plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        context.AddRange(user, recipe, catalog, personal);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new MealPlanRepository(context.MealPlans, new MealPlanCompositionReader(context));
        using var cancellation = new CancellationTokenSource();

        Assert.Null(await repository.GetForUpdateAsync(personal.Id, cancellation.Token));
        Assert.Null(await repository.GetForUpdateAsync(MealPlanId.New(), cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
        MealPlan? loaded = await repository.GetForUpdateAsync(catalog.Id, cancellation.Token);

        Assert.NotNull(loaded);
        MealPlanDay loadedDay = Assert.Single(loaded.Days);
        MealPlanMeal loadedMeal = Assert.Single(loadedDay.Meals);
        Assert.Multiple(
            () => Assert.Equal(day.Id, loadedDay.Id),
            () => Assert.Equal(catalog.Id, loadedDay.MealPlanId),
            () => Assert.Same(loaded, loadedDay.MealPlan),
            () => Assert.Equal(meal.Id, loadedMeal.Id),
            () => Assert.Equal(day.Id, loadedMeal.MealPlanDayId),
            () => Assert.Same(loadedDay, loadedMeal.Day),
            () => Assert.Equal(recipe.Id, loadedMeal.RecipeId),
            () => Assert.Equal(3, loadedMeal.Servings),
            () => Assert.Null(loadedMeal.RecipeSnapshot));
        Assert.Equal(3, context.ChangeTracker.Entries().Count());
        Assert.All(context.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Same(loaded, await repository.GetForUpdateAsync(catalog.Id, cancellation.Token));
        loaded.AddDay(2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        int[] storedDays = await context.Set<MealPlanDay>().Where(value => value.MealPlanId == catalog.Id)
            .OrderBy(value => value.DayNumber).Select(value => value.DayNumber).ToArrayAsync();
        Assert.Equal([1, 2], storedDays);
        Assert.True(await context.MealPlans.AsNoTracking().AnyAsync(plan => plan.Id == personal.Id && plan.UserId == user.Id));
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetForUpdateAsync(catalog.Id, cancellation.Token));
    }
}
