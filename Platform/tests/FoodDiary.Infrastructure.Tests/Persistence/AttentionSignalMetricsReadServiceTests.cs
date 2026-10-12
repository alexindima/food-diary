using FoodDiary.ReadModel.Composition.Dietologist;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class AttentionSignalMetricsReadServiceTests {
    [Fact]
    public async Task GetAsync_LoadsMetricsForAllRequestedClientsInOneBatch() {
        DateTime nowUtc = new(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);
        await using FoodDiaryDbContext context = CreateContext();
        var first = User.Create("first@example.com", "hash");
        var second = User.Create("second@example.com", "hash");
        var firstMeal = Meal.Create(first.Id, nowUtc.AddDays(-2));
        var secondMeal = Meal.Create(second.Id, nowUtc.AddDays(-1));
        var firstWeight = WeightEntry.Create(first.Id, nowUtc.AddDays(-3), 90);
        var secondWeight = WeightEntry.Create(second.Id, nowUtc.AddDays(-2), 75);
        context.AddRange(first, second, firstMeal, secondMeal, firstWeight, secondWeight);
        await context.SaveChangesAsync();
        var service = new AttentionSignalMetricsReadService(context);

        IReadOnlyList<AttentionSignalMetricsReadModel> result = await service.GetAsync(
            [first.Id, second.Id],
            nowUtc.AddDays(-7),
            nowUtc);

        Assert.Multiple(
            () => Assert.Equal(2, result.Count),
            () => Assert.Contains(result, item =>
                item.ClientUserId == first.Id.Value &&
                item.LastMealAtUtc == firstMeal.Date &&
                item.WeightPoints.Single().WeightKg == firstWeight.WeightKg),
            () => Assert.Contains(result, item =>
                item.ClientUserId == second.Id.Value &&
                item.LastMealAtUtc == secondMeal.Date &&
                item.WeightPoints.Single().WeightKg == secondWeight.WeightKg));
    }

    [Fact]
    public async Task GetAsync_WithNoClients_DoesNotQueryAndReturnsEmptyResult() {
        await using FoodDiaryDbContext context = CreateContext();
        var service = new AttentionSignalMetricsReadService(context);

        IReadOnlyList<AttentionSignalMetricsReadModel> result = await service.GetAsync(
            [],
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddDays(1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAsync_PreservesMissingAndAllTimeLastMealsAsync() {
        DateTime nowUtc = new(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);
        await using FoodDiaryDbContext context = CreateContext();
        var noMeals = User.Create("new-client@example.test", "hash");
        var oldMeals = User.Create("old-meal-client@example.test", "hash");
        var unrelated = User.Create("other-client@example.test", "hash");
        var oldMeal = Meal.Create(oldMeals.Id, nowUtc.AddDays(-30));
        context.AddRange(noMeals, oldMeals, unrelated, oldMeal, Meal.Create(unrelated.Id, nowUtc.AddDays(-1)));
        await context.SaveChangesAsync();

        var service = new AttentionSignalMetricsReadService(context);
        IReadOnlyList<AttentionSignalMetricsReadModel> result = await service.GetAsync(
            [noMeals.Id, oldMeals.Id], nowUtc.AddDays(-7), nowUtc);

        Assert.Equal(2, result.Count);
        AttentionSignalMetricsReadModel empty = Assert.Single(result, item => item.ClientUserId == noMeals.Id.Value);
        AttentionSignalMetricsReadModel old = Assert.Single(result, item => item.ClientUserId == oldMeals.Id.Value);
        Assert.Null(empty.LastMealAtUtc);
        Assert.Equal(oldMeal.Date, old.LastMealAtUtc);
        Assert.All(result, item => Assert.Empty(item.DailyCalories));
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FoodDiaryDbContext(options);
    }
}
