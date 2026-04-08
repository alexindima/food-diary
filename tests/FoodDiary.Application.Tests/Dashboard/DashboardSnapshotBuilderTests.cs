using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Dashboard;

public sealed class DashboardSnapshotBuilderTests {
    [Fact]
    public async Task BuildAsync_WithEmptyUserId_ReturnsValidationFailure() {
        var builder = new DashboardSnapshotBuilder(
            new StubSender(),
            new StubUserRepository(),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            new StubFastingSessionRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        var result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                Guid.Empty,
                new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
                "en",
                7,
                1,
                10),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubSender : ISender {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubUserRepository : IUserRepository {
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

    private sealed class StubWeightEntryRepository : IWeightEntryRepository {
        public Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByIdAsync(WeightEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(UserId userId, DateTime? dateFrom, DateTime? dateTo, int? limit, bool descending, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubExerciseEntryRepository : IExerciseEntryRepository {
        public Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExerciseEntry?> GetByIdAsync(ExerciseEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ExerciseEntry>> GetByDateRangeAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<double> GetTotalCaloriesBurnedAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => Task.FromResult(0.0);
    }

    private sealed class StubFastingSessionRepository : IFastingSessionRepository {
        public Task<FastingSession?> GetCurrentAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult<FastingSession?>(null);
        public Task<FastingSession?> GetByIdAsync(FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingSession>> GetHistoryAsync(UserId userId, DateTime from, DateTime to, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetCompletedCountAsync(UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetCurrentStreakAsync(UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FastingSession> AddAsync(FastingSession session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(FastingSession session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubWaistEntryRepository : IWaistEntryRepository {
        public Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByIdAsync(WaistEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(UserId userId, DateTime? dateFrom, DateTime? dateTo, int? limit, bool descending, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
