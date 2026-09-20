using FoodDiary.Modules.Meals.Infrastructure.Persistence.Meals;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.ReadModel.Composition.Meals;
using FoodDiary.Modules.Products.Infrastructure.Persistence.Products;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class TemporalRepositoryBoundaryTests {
    [Fact]
    public async Task MealRepository_AtMaximumDate_UsesInclusiveEndWithoutOverflow() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"meal-boundary-{Guid.NewGuid():N}@example.com", "hash");
        var meal = Meal.Create(user.Id, DateTime.MaxValue);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        var repository = new MealRepository(context.Meals, new MealProductNutritionQuery(context), new ProductSnapshotReadService(context.Products), new MealSourceSnapshotQuery(context));

        IReadOnlyList<Meal> period = await repository.GetByPeriodAsync(
            user.Id,
            DateTime.MaxValue,
            DateTime.MaxValue,
            CancellationToken.None);
        IReadOnlyList<DateTime> dates = await repository.GetDistinctMealDatesAsync(
            user.Id,
            DateTime.MaxValue,
            DateTime.MaxValue,
            CancellationToken.None);
        IReadOnlyList<Meal> day = await repository.GetWithItemsAndProductsAsync(
            user.Id,
            DateTime.MaxValue,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Single(period),
            () => Assert.Equal(DateTime.MaxValue.Date, Assert.Single(dates)),
            () => Assert.Single(day));
    }

    [Fact]
    public async Task HydrationRepository_AtMaximumDate_UsesInclusiveEndWithoutOverflow() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"hydration-boundary-{Guid.NewGuid():N}@example.com", "hash");
        var maximumUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        var entry = HydrationEntry.Create(user.Id, maximumUtc, 250);
        context.AddRange(user, entry);
        await context.SaveChangesAsync();
        var repository = new HydrationEntryRepository(context.HydrationEntries);

        IReadOnlyList<HydrationEntry> entries = await repository.GetByDateAsync(
            user.Id,
            maximumUtc,
            CancellationToken.None);
        int total = await repository.GetDailyTotalAsync(user.Id, maximumUtc, CancellationToken.None);
        IReadOnlyList<(DateTime Date, int TotalMl)> totals = await repository.GetDailyTotalsAsync(
            user.Id,
            maximumUtc,
            maximumUtc,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Single(entries),
            () => Assert.Equal(250, total),
            () => Assert.Equal((maximumUtc.Date, 250), Assert.Single(totals)));
    }

    [Fact]
    public async Task HydrationRepository_ExactBoundsDoNotExpandToUtcDates() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create("hydration-exact-bounds@example.com", "hash");
        var start = new DateTime(2026, 9, 19, 20, 0, 0, DateTimeKind.Utc);
        DateTime end = start.AddDays(1).AddTicks(-1);
        context.Users.Add(user);
        context.HydrationEntries.AddRange(
            HydrationEntry.Create(user.Id, start.AddTicks(-1), 1000),
            HydrationEntry.Create(user.Id, start, 100),
            HydrationEntry.Create(user.Id, end, 200),
            HydrationEntry.Create(user.Id, end.AddTicks(1), 2000));
        await context.SaveChangesAsync();
        var repository = new HydrationEntryRepository(context.HydrationEntries);
        IReadOnlyList<(DateTime Date, int TotalMl)> totals = await repository.GetDailyTotalsAsync(user.Id, start, end, CancellationToken.None, useExactBounds: true);
        Assert.Equal(300, totals.Sum(item => item.TotalMl));
        Assert.Equal(3300, (await repository.GetDailyTotalsAsync(user.Id, start, end)).Sum(item => item.TotalMl));
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
