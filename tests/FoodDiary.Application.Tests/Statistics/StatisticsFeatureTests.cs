using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Statistics;

public class StatisticsFeatureTests {
    [Fact]
    public async Task GetStatisticsQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetStatisticsQueryValidator();
        var query = new GetStatisticsQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository());
        var query = new GetStatisticsQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository());
        var query = new GetStatisticsQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithEmptyMeals_ReturnsSingleZeroBucket() {
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository());
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetStatisticsQuery(Guid.NewGuid(), from, to, 1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var bucket = Assert.Single(result.Value);
        Assert.Equal(0, bucket.TotalCalories);
        Assert.Equal(from, bucket.DateFrom);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithLocalDayUtcBoundaries_GroupsMealsByRequestedBoundary() {
        var userId = UserId.New();
        var localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        var localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        var includedMeal = Meal.Create(userId, localDayStartUtc.AddMinutes(30), MealType.Snack);
        includedMeal.ApplyNutrition(new MealNutritionUpdate(946, 59, 45, 76, 7, 0, true));
        var nextLocalDayMeal = Meal.Create(userId, localDayEndUtc.AddMinutes(1), MealType.Snack);
        nextLocalDayMeal.ApplyNutrition(new MealNutritionUpdate(41, 1, 0, 10, 3, 0, true));
        var handler = new GetStatisticsQueryHandler(new StaticMealRepository([includedMeal, nextLocalDayMeal]));
        var query = new GetStatisticsQuery(userId.Value, localDayStartUtc, localDayEndUtc, 1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var bucket = Assert.Single(result.Value);
        Assert.Equal(localDayStartUtc, bucket.DateFrom);
        Assert.Equal(localDayEndUtc, bucket.DateTo);
        Assert.Equal(946, bucket.TotalCalories);
    }

    private class NoopMealRepository : IMealRepository {
        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) => Task.FromResult(meal);
        public Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<Meal?>(null);

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((Items: (IReadOnlyList<Meal>)Array.Empty<Meal>(), TotalItems: 0));

        public virtual Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Meal>>(Array.Empty<Meal>());

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DateTime>>(Array.Empty<DateTime>());

        public Task<int> GetTotalMealCountAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
            UserId userId, DateTime date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Meal>>(Array.Empty<Meal>());
    }

    private sealed class StaticMealRepository(IReadOnlyList<Meal> meals) : NoopMealRepository {
        public override Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Meal>>(meals);
    }
}
