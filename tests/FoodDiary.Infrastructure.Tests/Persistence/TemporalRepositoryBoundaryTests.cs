using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Infrastructure.Persistence.Tracking;
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
        var repository = new MealRepository(context);

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
        var entry = HydrationEntry.Create(user.Id, DateTime.MaxValue, 250);
        context.AddRange(user, entry);
        await context.SaveChangesAsync();
        var repository = new HydrationEntryRepository(context);

        IReadOnlyList<HydrationEntry> entries = await repository.GetByDateAsync(
            user.Id,
            DateTime.MaxValue,
            CancellationToken.None);
        int total = await repository.GetDailyTotalAsync(user.Id, DateTime.MaxValue, CancellationToken.None);
        IReadOnlyList<(DateTime Date, int TotalMl)> totals = await repository.GetDailyTotalsAsync(
            user.Id,
            DateTime.MaxValue,
            DateTime.MaxValue,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Single(entries),
            () => Assert.Equal(250, total),
            () => Assert.Equal((DateTime.MaxValue.Date, 250), Assert.Single(totals)));
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
