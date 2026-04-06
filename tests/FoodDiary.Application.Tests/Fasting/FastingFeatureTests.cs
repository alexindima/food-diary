using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Common;

using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public class FastingFeatureTests {
    private static readonly DateTime FixedNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StartFasting_WithValidData_CreatesSession() {
        var user = User.Create("user@example.com", "hash");
        var repo = new InMemoryFastingRepository();
        var handler = new StartFastingCommandHandler(repo, new StubUserRepository(user), new FixedDateTimeProvider());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("F16_8", result.Value.Protocol);
        Assert.Equal(16, result.Value.PlannedDurationHours);
        Assert.False(result.Value.IsCompleted);
    }

    [Fact]
    public async Task StartFasting_WhenAlreadyActive_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var existing = FastingSession.Create(user.Id, FastingProtocol.F16_8, 16, FixedNow);
        var repo = new InMemoryFastingRepository(current: existing);
        var handler = new StartFastingCommandHandler(repo, new StubUserRepository(user), new FixedDateTimeProvider());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F18_6", null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyActive", result.Error.Code);
    }

    [Fact]
    public async Task StartFasting_WithInvalidProtocol_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingRepository(), new StubUserRepository(user), new FixedDateTimeProvider());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "InvalidProtocol", null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WithNullUserId_ReturnsFailure() {
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingRepository(), new StubUserRepository(null), new FixedDateTimeProvider());

        var result = await handler.Handle(
            new StartFastingCommand(null, "F16_8", null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task EndFasting_WhenActiveSession_Succeeds() {
        var userId = UserId.New();
        var session = FastingSession.Create(userId, FastingProtocol.F16_8, 16, FixedNow);
        var repo = new InMemoryFastingRepository(current: session);
        var handler = new EndFastingCommandHandler(repo, new FixedDateTimeProvider());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsCompleted);
    }

    [Fact]
    public async Task EndFasting_WhenNoActiveSession_ReturnsFailure() {
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingRepository(), new FixedDateTimeProvider());

        var result = await handler.Handle(
            new EndFastingCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NoActiveSession", result.Error.Code);
    }

    private sealed class InMemoryFastingRepository(FastingSession? current = null) : IFastingSessionRepository {
        public Task<FastingSession?> GetCurrentAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(current);
        public Task<FastingSession> AddAsync(FastingSession session, CancellationToken ct = default) => Task.FromResult(session);
        public Task UpdateAsync(FastingSession session, CancellationToken ct = default) => Task.CompletedTask;
        public Task<FastingSession?> GetByIdAsync(FastingSessionId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingSession>> GetHistoryAsync(UserId userId, DateTime from, DateTime to, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetCompletedCountAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetCurrentStreakAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) => Task.FromResult(user);
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

    private sealed class FixedDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => FixedNow;
    }
}
