using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence.Meals;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class MealRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesDateFilterAndKeepsPagingMetadata() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var olderMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        var newerMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc));
        var filteredOutMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(olderMeal, newerMeal, filteredOutMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 1,
            dateFrom: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));

        var item = Assert.Single(items);
        Assert.Equal(2, totalItems);
        Assert.Equal(newerMeal.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_IncludesMealsThroughoutDateToDay() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-time-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var morningMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 2, 8, 15, 0, DateTimeKind.Utc));
        var eveningMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 2, 21, 30, 0, DateTimeKind.Utc));
        var nextDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(morningMeal, eveningMeal, nextDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 10,
            dateFrom: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, totalItems);
        Assert.Collection(
            items,
            item => Assert.Equal(eveningMeal.Id, item.Id),
            item => Assert.Equal(morningMeal.Id, item.Id));
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_UsesExactUtcInstantsForLocalDayBoundaries() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-local-day-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var previousLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 18, 48, 0, DateTimeKind.Utc));
        var firstLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 20, 30, 0, DateTimeKind.Utc));
        var lastLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 5, 19, 30, 0, DateTimeKind.Utc));
        var nextLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 5, 20, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(previousLocalDayMeal, firstLocalDayMeal, lastLocalDayMeal, nextLocalDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 10,
            dateFrom: new DateTime(2026, 5, 4, 20, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 5, 5, 19, 59, 59, 999, DateTimeKind.Utc));

        Assert.Equal(2, totalItems);
        Assert.Collection(
            items,
            item => Assert.Equal(lastLocalDayMeal.Id, item.Id),
            item => Assert.Equal(firstLocalDayMeal.Id, item.Id));
    }

    [RequiresDockerFact]
    public async Task GetDistinctMealDatesAsync_ReturnsDistinctDaysForTimedMeals() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-dates-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Meals.AddRange(
            Meal.Create(user.Id, new DateTime(2026, 5, 2, 8, 15, 0, DateTimeKind.Utc)),
            Meal.Create(user.Id, new DateTime(2026, 5, 2, 21, 30, 0, DateTimeKind.Utc)),
            Meal.Create(user.Id, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var dates = await repository.GetDistinctMealDatesAsync(
            user.Id,
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Collection(
            dates,
            date => Assert.Equal(new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), date),
            date => Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), date));
    }

    [RequiresDockerFact]
    public async Task GetWithItemsAndProductsAsync_FindsMealsByDayWhenDateHasTime() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-products-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 2, 13, 45, 0, DateTimeKind.Utc));
        var otherDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(meal, otherDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var meals = await repository.GetWithItemsAndProductsAsync(
            user.Id,
            new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));

        var actualMeal = Assert.Single(meals);
        Assert.Equal(meal.Id, actualMeal.Id);
    }

    [RequiresDockerFact]
    public async Task GetByPeriodAsync_IncludesAiSessionItemsForExport() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-ai-export-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 13, 45, 0, DateTimeKind.Utc));
        meal.AddAiSession(
            null,
            AiRecognitionSource.Text,
            new DateTime(2026, 5, 4, 13, 46, 0, DateTimeKind.Utc),
            null,
            [
                MealAiItemData.Create("Rice", "Рис", 445, "g", 905, 58, 45, 66, 4, 0),
            ]);

        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var meals = await repository.GetByPeriodAsync(
            user.Id,
            new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 4, 23, 59, 59, DateTimeKind.Utc));

        var actualMeal = Assert.Single(meals);
        var session = Assert.Single(actualMeal.AiSessions);
        var item = Assert.Single(session.Items);
        Assert.Equal("Rice", item.NameEn);
    }
}
