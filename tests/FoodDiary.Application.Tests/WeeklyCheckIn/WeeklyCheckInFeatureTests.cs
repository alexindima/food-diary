using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.WeeklyCheckIn;

public class WeeklyCheckInFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetWeeklyCheckIn_WithNullUserId_ReturnsFailure() {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetWeeklyCheckInQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WhenUserNotFound_ReturnsFailure() {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetWeeklyCheckInQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WithValidUser_ReturnsModel() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = CreateHandler(userRepo: userRepo);

        var result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.ThisWeek);
        Assert.NotNull(result.Value.LastWeek);
        Assert.NotNull(result.Value.Trends);
        Assert.NotNull(result.Value.Suggestions);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WithNoData_ReturnsZeroSummaries() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = CreateHandler(userRepo: userRepo);

        var result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.ThisWeek.TotalCalories);
        Assert.Equal(0, result.Value.ThisWeek.MealsLogged);
    }

    private static GetWeeklyCheckInQueryHandler CreateHandler(
        IMealRepository? mealRepo = null,
        IWeightEntryRepository? weightRepo = null,
        IWaistEntryRepository? waistRepo = null,
        IHydrationEntryRepository? hydrationRepo = null,
        IUserRepository? userRepo = null) =>
        new(
            mealRepo ?? new StubMealRepository(),
            weightRepo ?? new StubWeightEntryRepository(),
            waistRepo ?? new StubWaistEntryRepository(),
            hydrationRepo ?? new StubHydrationEntryRepository(),
            userRepo ?? new InMemoryUserRepository(),
            new StubDateTimeProvider());

    // Test Doubles

    private sealed class StubMealRepository : IMealRepository {
        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Meal>>([]);

        public Task<Meal> AddAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Meal?> GetByIdAsync(MealId id, UserId userId, bool includeItems = false, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(UserId userId, int page, int limit, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubWeightEntryRepository : IWeightEntryRepository {
        public Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WeightEntry>>([]);

        public Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(WeightEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(WeightEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByIdAsync(WeightEntryId id, UserId userId, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(UserId userId, DateTime? dateFrom, DateTime? dateTo, int? limit, bool descending, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubWaistEntryRepository : IWaistEntryRepository {
        public Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WaistEntry>>([]);

        public Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(WaistEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(WaistEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByIdAsync(WaistEntryId id, UserId userId, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(UserId userId, DateTime? dateFrom, DateTime? dateTo, int? limit, bool descending, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubHydrationEntryRepository : IHydrationEntryRepository {
        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<(DateTime Date, int TotalMl)>>([]);

        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(HydrationEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(HydrationEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class InMemoryUserRepository : IUserRepository {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => Today;
    }
}
