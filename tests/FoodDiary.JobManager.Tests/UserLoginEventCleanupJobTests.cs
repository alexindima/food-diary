using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserLoginEventCleanupJobTests : IDisposable {
    private readonly JobExecutionStateTracker _stateTracker = new();

    [Fact]
    public async Task Execute_WhenDisabled_DoesNotDeleteEvents() {
        var repository = new RecordingUserLoginEventRepository();
        UserLoginEventCleanupJob job = CreateJob(
            repository,
            new UserLoginEventCleanupOptions { Enabled = false });

        await job.Execute();

        Assert.Equal(0, repository.DeleteCallCount);
        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("users.login_events_cleanup");
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
    }

    [Fact]
    public async Task Execute_WhenEnabled_DeletesExpiredEventsUntilBatchIsNotFull() {
        var repository = new RecordingUserLoginEventRepository(10, 10, 3);
        var nowUtc = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);
        UserLoginEventCleanupJob job = CreateJob(
            repository,
            new UserLoginEventCleanupOptions {
                Enabled = true,
                RetentionDays = 30,
                BatchSize = 10,
            },
            new FixedTimeProvider(nowUtc));

        await job.Execute();

        Assert.Equal(3, repository.DeleteCallCount);
        Assert.Equal([10, 10, 10], repository.BatchSizes);
        Assert.Equal([10, 10, 3], repository.DeletedCounts);
        Assert.All(repository.Cutoffs, cutoff => Assert.Equal(nowUtc.AddDays(-30), cutoff));
    }

    [Fact]
    public async Task Execute_WhenRepositoryThrows_RecordsFailureAndRethrows() {
        var repository = new ThrowingUserLoginEventRepository();
        UserLoginEventCleanupJob job = CreateJob(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("users.login_events_cleanup");
        Assert.Equal(1, snapshot!.Value.ConsecutiveFailures);
    }

    [Fact]
    public async Task Execute_WhenCanceledAfterPartialDelete_RecordsCanceledAndRethrows() {
        using var cts = new CancellationTokenSource();
        var repository = new CancelingUserLoginEventRepository(cts, 10);
        UserLoginEventCleanupJob job = CreateJob(
            repository,
            new UserLoginEventCleanupOptions {
                Enabled = true,
                RetentionDays = 30,
                BatchSize = 10,
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() => job.Execute(cts.Token));

        Assert.Equal([10], repository.DeletedCounts);
        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("users.login_events_cleanup");
        Assert.NotNull(snapshot);
        Assert.Multiple(
            () => Assert.Equal(0, snapshot.Value.ConsecutiveFailures),
            () => Assert.Null(snapshot.Value.LastFailedAtUtc),
            () => Assert.Null(snapshot.Value.LastSucceededAtUtc));
    }

    private UserLoginEventCleanupJob CreateJob(
        IUserLoginEventRepository repository,
        UserLoginEventCleanupOptions? options = null,
        TimeProvider? timeProvider = null) =>
        new(
            new AuthenticationLoginEventCleanupService(repository),
            Options.Create(options ?? new UserLoginEventCleanupOptions()),
            new JobExecutionObserver(timeProvider ?? new FixedTimeProvider(new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc)), _stateTracker),
            NullLogger<UserLoginEventCleanupJob>.Instance);

    public void Dispose() => _stateTracker.Dispose();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private class RecordingUserLoginEventRepository(params int[] deleteCounts) : IUserLoginEventRepository {
        private readonly Queue<int> _deleteCounts = new(deleteCounts);

        public int DeleteCallCount { get; private set; }
        public List<int> BatchSizes { get; } = [];
        public List<int> DeletedCounts { get; } = [];
        public List<DateTime> Cutoffs { get; } = [];

        public Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
            int page,
            int limit,
            Guid? userId,
            string? search,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            BatchSizes.Add(batchSize);
            Cutoffs.Add(olderThanUtc);
            int deleteCount = _deleteCounts.Count == 0 ? 0 : _deleteCounts.Dequeue();
            DeletedCounts.Add(deleteCount);
            return Task.FromResult(deleteCount);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingUserLoginEventRepository : RecordingUserLoginEventRepository {
        public override Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("cleanup failed");
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingUserLoginEventRepository(
        CancellationTokenSource cancellationTokenSource,
        params int[] deleteCounts) : RecordingUserLoginEventRepository(deleteCounts) {
        public override async Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            int deleted = await base.DeleteOlderThanAsync(olderThanUtc, batchSize, cancellationToken).ConfigureAwait(false);
            await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            return deleted;
        }
    }
}
