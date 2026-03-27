using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.Entities.Meals;
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

    private sealed class NoopMealRepository : IMealRepository {
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

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Meal>>(Array.Empty<Meal>());
    }
}
