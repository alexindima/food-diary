using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesDateFilterAndKeepsPagingMetadata() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
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

        (IReadOnlyList<Meal>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 1,
            filters: new MealQueryFilters(
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)));

        Meal item = Assert.Single(items);
        Assert.Equal(2, totalItems);
        Assert.Equal(newerMeal.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_IncludesMealsThroughoutDateToDay() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
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

        (IReadOnlyList<Meal>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 10,
            filters: new MealQueryFilters(
                new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(2, totalItems);
        Assert.Collection(
            items,
            item => Assert.Equal(eveningMeal.Id, item.Id),
            item => Assert.Equal(morningMeal.Id, item.Id));
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_UsesExactUtcInstantsForLocalDayBoundaries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
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

        (IReadOnlyList<Meal>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 10,
            filters: new MealQueryFilters(
                new DateTime(2026, 5, 4, 20, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 5, 19, 59, 59, 999, DateTimeKind.Utc)));

        Assert.Equal(2, totalItems);
        Assert.Collection(
            items,
            item => Assert.Equal(lastLocalDayMeal.Id, item.Id),
            item => Assert.Equal(firstLocalDayMeal.Id, item.Id));
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesStructuredFilters() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-filters-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        (Meal breakfastWithImage, Meal lunchWithAi, Meal dinnerNoImage) =
            await SeedMealFiltersAsync(context, user.Id);
        var repository = new MealRepository(context);

        IReadOnlyList<MealId> mealTypeIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            MealTypes: [MealType.Breakfast, MealType.Lunch]));
        IReadOnlyList<MealId> calorieIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            CaloriesFrom: 600,
            CaloriesTo: 700));
        IReadOnlyList<MealId> withImageIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasImage: true));
        IReadOnlyList<MealId> withoutImageIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasImage: false));
        IReadOnlyList<MealId> withAiIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasAiSession: true));
        IReadOnlyList<MealId> withoutAiIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasAiSession: false));

        AssertIds([breakfastWithImage.Id, lunchWithAi.Id], mealTypeIds);
        Assert.Equal([lunchWithAi.Id], calorieIds);
        Assert.Equal([breakfastWithImage.Id], withImageIds);
        AssertIds([lunchWithAi.Id, dinnerNoImage.Id], withoutImageIds);
        Assert.Equal([lunchWithAi.Id], withAiIds);
        AssertIds([breakfastWithImage.Id, dinnerNoImage.Id], withoutAiIds);
    }

    [RequiresDockerFact]
    public async Task GetDistinctMealDatesAsync_ReturnsDistinctDaysForTimedMeals() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-dates-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Meals.AddRange(
            Meal.Create(user.Id, new DateTime(2026, 5, 2, 8, 15, 0, DateTimeKind.Utc)),
            Meal.Create(user.Id, new DateTime(2026, 5, 2, 21, 30, 0, DateTimeKind.Utc)),
            Meal.Create(user.Id, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        IReadOnlyList<DateTime> dates = await repository.GetDistinctMealDatesAsync(
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
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
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

        IReadOnlyList<Meal> meals = await repository.GetWithItemsAndProductsAsync(
            user.Id,
            new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));

        Meal actualMeal = Assert.Single(meals);
        Assert.Equal(meal.Id, actualMeal.Id);
    }

    [RequiresDockerFact]
    public async Task GetByPeriodAsync_IncludesAiSessionItemsForExport() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-ai-export-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 13, 45, 0, DateTimeKind.Utc));
        meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            new DateTime(2026, 5, 4, 13, 46, 0, DateTimeKind.Utc),
            notes: null,
            [
                MealAiItemData.Create("Rice", "Ð Ð¸Ñ", 445, "g", 905, 58, 45, 66, 4, 0),
            ]);

        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        IReadOnlyList<Meal> meals = await repository.GetByPeriodAsync(
            user.Id,
            new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 4, 23, 59, 59, DateTimeKind.Utc));

        Meal actualMeal = Assert.Single(meals);
        MealAiSession session = Assert.Single(actualMeal.AiSessions);
        MealAiItem item = Assert.Single(session.Items);
        Assert.Equal("Rice", item.NameEn);
    }

    private static MealNutritionUpdate CreateManualNutrition(double calories) =>
        new(
            TotalCalories: calories,
            TotalProteins: 1,
            TotalFats: 1,
            TotalCarbs: 1,
            TotalFiber: 0,
            TotalAlcohol: 0,
            IsAutoCalculated: false,
            ManualCalories: calories,
            ManualProteins: 1,
            ManualFats: 1,
            ManualCarbs: 1,
            ManualFiber: 0,
            ManualAlcohol: 0);

    private static async Task<IReadOnlyList<MealId>> GetMealIdsAsync(
        MealRepository repository,
        UserId userId,
        MealQueryFilters filters) {
        (IReadOnlyList<Meal> items, int _) = await repository.GetPagedAsync(
            userId,
            page: 1,
            limit: 50,
            filters: filters).ConfigureAwait(false);

        return [.. items.Select(item => item.Id)];
    }

    private static async Task<(Meal BreakfastWithImage, Meal LunchWithAi, Meal DinnerNoImage)> SeedMealFiltersAsync(
        FoodDiaryDbContext context,
        UserId userId) {
        var breakfastWithImage = Meal.Create(
            userId,
            new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            MealType.Breakfast,
            imageUrl: "https://cdn.example.com/breakfast.webp");
        breakfastWithImage.ApplyNutrition(CreateManualNutrition(calories: 350));

        var lunchWithAi = Meal.Create(userId, new DateTime(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        lunchWithAi.ApplyNutrition(CreateManualNutrition(calories: 650));
        lunchWithAi.AddAiSession(imageAssetId: null, AiRecognitionSource.Text, new DateTime(2026, 6, 1, 13, 5, 0, DateTimeKind.Utc), notes: null, items: []);

        var dinnerNoImage = Meal.Create(userId, new DateTime(2026, 6, 1, 19, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinnerNoImage.ApplyNutrition(CreateManualNutrition(calories: 820));

        context.Meals.AddRange(breakfastWithImage, lunchWithAi, dinnerNoImage);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return (breakfastWithImage, lunchWithAi, dinnerNoImage);
    }

    private static void AssertIds(IReadOnlyCollection<MealId> expected, IReadOnlyCollection<MealId> actual) =>
        Assert.Equal(
            [.. expected.Select(id => id.Value).Order()],
            [.. actual.Select(id => id.Value).Order()]);
}
