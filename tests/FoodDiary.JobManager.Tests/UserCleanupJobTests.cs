using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

public sealed class UserCleanupJobTests : IDisposable {
    private readonly JobExecutionStateTracker _stateTracker = new();

    [Fact]
    public async Task Execute_WhenNoDeletedUsers_RecordsSuccess() {
        var cleanup = new StubUserCleanupService(usersPerBatch: 0);
        var job = CreateJob(cleanup);

        await job.Execute();

        var snapshot = _stateTracker.GetSnapshot("users.cleanup");
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.Value.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WithDeletedUsers_CleansUpInBatches() {
        var cleanup = new StubUserCleanupService(usersPerBatch: 2, totalAvailable: 3);
        var options = new UserCleanupOptions { BatchSize = 2, RetentionDays = 30 };
        var job = CreateJob(cleanup, options);

        await job.Execute();

        Assert.Equal(3, cleanup.TotalDeleted);
        Assert.Equal(2, cleanup.CallCount);
    }

    [Fact]
    public async Task Execute_WithReassignUserId_PassesItToService() {
        var reassignId = Guid.NewGuid();
        var cleanup = new StubUserCleanupService(usersPerBatch: 0);
        var options = new UserCleanupOptions { ReassignUserId = reassignId.ToString() };
        var job = CreateJob(cleanup, options);

        await job.Execute();

        Assert.Equal(reassignId, cleanup.LastReassignUserId);
    }

    [Fact]
    public async Task Execute_WithInvalidReassignUserId_PassesNull() {
        var cleanup = new StubUserCleanupService(usersPerBatch: 0);
        var options = new UserCleanupOptions { ReassignUserId = "not-a-guid" };
        var job = CreateJob(cleanup, options);

        await job.Execute();

        Assert.Null(cleanup.LastReassignUserId);
    }

    [Fact]
    public async Task Execute_WhenServiceThrows_RecordsFailure() {
        var cleanup = new ThrowingUserCleanupService();
        var job = CreateJob(cleanup);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        var snapshot = _stateTracker.GetSnapshot("users.cleanup");
        Assert.Equal(1, snapshot!.Value.ConsecutiveFailures);
    }

    private UserCleanupJob CreateJob(
        IUserCleanupService cleanupService,
        UserCleanupOptions? options = null) {
        return new UserCleanupJob(
            cleanupService,
            Options.Create(options ?? new UserCleanupOptions()),
            new FixedDateTimeProvider(),
            _stateTracker,
            NullLogger<UserCleanupJob>.Instance);
    }

    public void Dispose() => _stateTracker.Dispose();

    private sealed class StubUserCleanupService(int usersPerBatch, int totalAvailable = 0) : IUserCleanupService {
        public int TotalDeleted { get; private set; }
        public int CallCount { get; private set; }
        public Guid? LastReassignUserId { get; private set; }

        public Task<int> CleanupDeletedUsersAsync(
            DateTime olderThanUtc, int batchSize, Guid? reassignUserId, CancellationToken ct = default) {
            CallCount++;
            LastReassignUserId = reassignUserId;
            var remaining = totalAvailable - TotalDeleted;
            var toDelete = Math.Min(Math.Min(usersPerBatch, batchSize), remaining);
            TotalDeleted += toDelete;
            return Task.FromResult(toDelete);
        }
    }

    private sealed class ThrowingUserCleanupService : IUserCleanupService {
        public Task<int> CleanupDeletedUsersAsync(
            DateTime olderThanUtc, int batchSize, Guid? reassignUserId, CancellationToken ct = default) =>
            throw new InvalidOperationException("DB error");
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
    }
}
