using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Modules.Meals.Application.Abstractions.Models;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Modules.Meals.Infrastructure.Persistence.Meals;
using FoodDiary.Modules.Products.Infrastructure.Persistence.Products;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.Meals;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Meals.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealDaySummaryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DailyTotals_UseLocalHalfOpenDays_AndIgnorePageFiltersAndOtherUsers() {
        foreach ((string zoneId, DateOnly day) in new (string, DateOnly)[] {
            ("UTC", new(2026, 1, 1)), ("Asia/Tbilisi", new(2026, 9, 22)),
            ("Asia/Kathmandu", new(2026, 1, 1)), ("Pacific/Kiritimati", new(2026, 1, 1)),
            ("Pacific/Pago_Pago", new(2026, 1, 1)), ("America/New_York", new(2026, 3, 8)),
            ("America/New_York", new(2026, 11, 1)), ("Europe/Berlin", new(2026, 3, 29)),
            ("Europe/Berlin", new(2026, 10, 25)), ("Australia/Lord_Howe", new(2026, 4, 5)),
        }) {
            await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
            var user = User.Create($"daily-{Guid.NewGuid():N}@example.com", "hash");
            var other = User.Create($"other-{Guid.NewGuid():N}@example.com", "hash");
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            DateTime start = LocalCalendar.StartOfDayUtc(day, zone);
            DateTime end = LocalCalendar.StartOfDayUtc(day.AddDays(1), zone);
            Meal[] meals = [CreateMeal(user, start.AddSeconds(-1), 1000), CreateMeal(user, start, 100),
                CreateMeal(user, start.AddHours(1), 200), CreateMeal(user, end.AddSeconds(-1), 300),
                CreateMeal(user, end, 400), CreateMeal(other, start, 5000),
                CreateMeal(user, start.AddDays(-20), 6000)];
            context.AddRange(user, other);
            context.AddRange(meals);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var repository = new MealRepository(context.Meals, new MealProductNutritionQuery(context), new ProductSnapshotReadService(context.Products), new MealSourceSnapshotQuery(context));
            IReadOnlyList<MealDaySummary> totals = await repository.GetDaySummariesAsync(user.Id, [day, day, day.AddDays(1)], zone);
            MealDaySummary total = Assert.Single(totals, value => value.Date == day);
            Assert.Multiple(() => Assert.Equal(600, total.TotalCalories), () => Assert.Equal(3, total.MealCount),
                () => Assert.Equal(400, Assert.Single(totals, value => value.Date == day.AddDays(1)).TotalCalories),
                () => Assert.Equal(2, totals.Count));
            var filters = new MealQueryFilters(start, end.AddTicks(-1), [MealType.Breakfast], CaloriesFrom: 150);
            (IReadOnlyList<MealProjectionReadModel> firstItems, int firstCount) = await repository.GetPagedMealProjectionsAsync(user.Id, 1, 1, filters);
            (IReadOnlyList<MealProjectionReadModel> secondItems, int secondCount) = await repository.GetPagedMealProjectionsAsync(user.Id, 2, 1, filters);
            Assert.Single(firstItems);
            Assert.Single(secondItems);
            Assert.Equal(2, firstCount);
            Assert.Equal(2, secondCount);
            Assert.NotEqual(firstItems[0].Id, secondItems[0].Id);
            Assert.Equal(totals, await repository.GetDaySummariesAsync(user.Id, [day, day.AddDays(1)], zone));
            Assert.Empty(context.ChangeTracker.Entries());
            Assert.Empty(await repository.GetDaySummariesAsync(user.Id, [], zone));
        }
    }

    private static Meal CreateMeal(User user, DateTime date, double calories) {
        var meal = Meal.Create(user.Id, date, MealType.Breakfast);
        meal.ApplyNutrition(new MealNutritionUpdate(calories, 0, 0, 0, 0, 0, IsAutoCalculated: false));
        return meal;
    }
}
