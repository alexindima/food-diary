using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Statistics;

[ExcludeFromCodeCoverage]
public class StatisticsFeatureTests {
    [Fact]
    public async Task GetStatisticsQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetStatisticsQueryValidator();
        var query = new GetStatisticsQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository(), new NoopUserRepository());
        var query = new GetStatisticsQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository(), new NoopUserRepository());
        var query = new GetStatisticsQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithEmptyMeals_ReturnsSingleZeroBucket() {
        var user = User.Create("statistics-empty@example.com", "hash");
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository(), new SingleUserRepository(user));
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetStatisticsQuery(user.Id.Value, from, to, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Success(result);
        AggregatedStatisticsModel bucket = Assert.Single(result.Value);
        Assert.Equal(0, bucket.TotalCalories);
        Assert.Equal(0, bucket.TotalProteins);
        Assert.Equal(0, bucket.TotalFats);
        Assert.Equal(0, bucket.TotalCarbs);
        Assert.Equal(0, bucket.TotalFiber);
        Assert.Equal(from, bucket.DateFrom);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("statistics-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetStatisticsQueryHandler(new NoopMealRepository(), new SingleUserRepository(user));
        var query = new GetStatisticsQuery(user.Id.Value, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithMultiDayBucket_ReturnsTotalsAndDailyAverages() {
        var user = User.Create("statistics-multiday@example.com", "hash");
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 2, 23, 59, 59, DateTimeKind.Utc);
        var meal = Meal.Create(user.Id, from.AddHours(12), MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(1000, 100, 50, 200, 20, 0, IsAutoCalculated: true));
        var handler = new GetStatisticsQueryHandler(new StaticMealRepository([meal]), new SingleUserRepository(user));
        var query = new GetStatisticsQuery(user.Id.Value, from, to, 2);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Success(result);
        AggregatedStatisticsModel bucket = Assert.Single(result.Value);
        Assert.Equal(1000, bucket.TotalCalories);
        Assert.Equal(100, bucket.TotalProteins);
        Assert.Equal(50, bucket.TotalFats);
        Assert.Equal(200, bucket.TotalCarbs);
        Assert.Equal(20, bucket.TotalFiber);
        Assert.Equal(50, bucket.AverageProteins);
        Assert.Equal(25, bucket.AverageFats);
        Assert.Equal(100, bucket.AverageCarbs);
        Assert.Equal(10, bucket.AverageFiber);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithLocalDayUtcBoundaries_GroupsMealsByRequestedBoundary() {
        var user = User.Create("statistics-boundaries@example.com", "hash");
        DateTime localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        DateTime localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        var includedMeal = Meal.Create(user.Id, localDayStartUtc.AddMinutes(30), MealType.Snack);
        includedMeal.ApplyNutrition(new MealNutritionUpdate(946, 59, 45, 76, 7, 0, IsAutoCalculated: true));
        var nextLocalDayMeal = Meal.Create(user.Id, localDayEndUtc.AddMinutes(1), MealType.Snack);
        nextLocalDayMeal.ApplyNutrition(new MealNutritionUpdate(41, 1, 0, 10, 3, 0, IsAutoCalculated: true));
        var handler = new GetStatisticsQueryHandler(new StaticMealRepository([includedMeal, nextLocalDayMeal]), new SingleUserRepository(user));
        var query = new GetStatisticsQuery(user.Id.Value, localDayStartUtc, localDayEndUtc, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Success(result);
        AggregatedStatisticsModel bucket = Assert.Single(result.Value);
        Assert.Equal(localDayStartUtc, bucket.DateFrom);
        Assert.Equal(localDayEndUtc, bucket.DateTo);
        Assert.Equal(946, bucket.TotalCalories);
        Assert.Equal(59, bucket.AverageProteins);
        Assert.Equal(45, bucket.AverageFats);
        Assert.Equal(76, bucket.AverageCarbs);
        Assert.Equal(7, bucket.AverageFiber);
        Assert.Equal(59, bucket.TotalProteins);
        Assert.Equal(45, bucket.TotalFats);
        Assert.Equal(76, bucket.TotalCarbs);
        Assert.Equal(7, bucket.TotalFiber);
    }

    [ExcludeFromCodeCoverage]
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
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((Items: (IReadOnlyList<Meal>)[], TotalItems: 0));

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

    [ExcludeFromCodeCoverage]
    private sealed class StaticMealRepository(IReadOnlyList<Meal> meals) : NoopMealRepository {
        public override Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Meal>>(meals);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopUserRepository : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
