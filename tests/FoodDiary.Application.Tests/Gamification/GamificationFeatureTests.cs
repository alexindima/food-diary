using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Gamification.Queries.GetGamification;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Gamification;

public class GamificationFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetGamification_WithNullUserId_ReturnsFailure() {
        var handler = new GetGamificationQueryHandler(
            new StubMealRepository(), new InMemoryUserRepository(), new StubDateTimeProvider());

        var result = await handler.Handle(
            new GetGamificationQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetGamification_WhenUserNotFound_ReturnsFailure() {
        var handler = new GetGamificationQueryHandler(
            new StubMealRepository(), new InMemoryUserRepository(), new StubDateTimeProvider());

        var result = await handler.Handle(
            new GetGamificationQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetGamification_WithValidUser_ReturnsModel() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var mealRepo = new StubMealRepository();
        mealRepo.SetDistinctDates(new List<DateTime> { Today, Today.AddDays(-1), Today.AddDays(-2) });
        mealRepo.SetTotalMealCount(15);

        var handler = new GetGamificationQueryHandler(mealRepo, userRepo, new StubDateTimeProvider());

        var result = await handler.Handle(
            new GetGamificationQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.CurrentStreak);
        Assert.Equal(15, result.Value.TotalMealsLogged);
    }

    [Fact]
    public async Task GetGamification_WithNoMeals_ReturnsZeroStreaks() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new GetGamificationQueryHandler(
            new StubMealRepository(), userRepo, new StubDateTimeProvider());

        var result = await handler.Handle(
            new GetGamificationQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.CurrentStreak);
        Assert.Equal(0, result.Value.TotalMealsLogged);
    }

    // ── Test Doubles ──

    private sealed class StubMealRepository : IMealRepository {
        private List<DateTime> _distinctDates = [];
        private int _totalMealCount;

        public void SetDistinctDates(List<DateTime> dates) => _distinctDates = dates;
        public void SetTotalMealCount(int count) => _totalMealCount = count;

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DateTime>>(_distinctDates);

        public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_totalMealCount);

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Meal>>([]);

        public Task<Meal> AddAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Meal?> GetByIdAsync(MealId id, UserId userId, bool includeItems = false, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(UserId userId, int page, int limit, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
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
