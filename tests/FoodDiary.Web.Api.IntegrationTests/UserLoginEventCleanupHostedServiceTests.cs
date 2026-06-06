using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class UserLoginEventCleanupHostedServiceTests {
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotDeleteLoginEvents() {
        var repository = new RecordingUserLoginEventRepository();
        using ServiceProvider provider = BuildServiceProvider(repository);
        var service = new UserLoginEventCleanupHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new UserLoginEventCleanupOptions {
                Enabled = false,
                RetentionDays = 30,
                BatchSize = 10,
                PollIntervalHours = 1,
            }),
            NullLogger<UserLoginEventCleanupHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_DeletesExpiredLoginEventsUntilBatchIsNotFull() {
        var repository = new RecordingUserLoginEventRepository(10, 10, 3);
        using ServiceProvider provider = BuildServiceProvider(repository);
        var service = new UserLoginEventCleanupHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new UserLoginEventCleanupOptions {
                Enabled = true,
                RetentionDays = 30,
                BatchSize = 10,
                PollIntervalHours = 1,
            }),
            NullLogger<UserLoginEventCleanupHostedService>.Instance);
        DateTime beforeStartUtc = DateTime.UtcNow;

        await service.StartAsync(CancellationToken.None);
        await repository.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        DateTime afterStartUtc = DateTime.UtcNow;
        Assert.Equal(3, repository.DeleteCallCount);
        Assert.Equal([10, 10, 10], repository.BatchSizes);
        Assert.Equal([10, 10, 3], repository.DeletedCounts);
        Assert.All(repository.Cutoffs, cutoff => {
            Assert.True(cutoff >= beforeStartUtc.AddDays(-30).AddSeconds(-1));
            Assert.True(cutoff <= afterStartUtc.AddDays(-30).AddSeconds(1));
        });
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryThrows_ContinuesUntilStopped() {
        var repository = new ThrowingUserLoginEventRepository();
        using ServiceProvider provider = BuildServiceProvider(repository);
        var service = new UserLoginEventCleanupHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new UserLoginEventCleanupOptions {
                Enabled = true,
                RetentionDays = 30,
                BatchSize = 10,
                PollIntervalHours = 1,
            }),
            NullLogger<UserLoginEventCleanupHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await repository.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, repository.DeleteCallCount);
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryObservesCancellation_StopsCleanly() {
        var repository = new CancelingUserLoginEventRepository();
        using ServiceProvider provider = BuildServiceProvider(repository);
        var service = new UserLoginEventCleanupHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new UserLoginEventCleanupOptions {
                Enabled = true,
                RetentionDays = 30,
                BatchSize = 10,
                PollIntervalHours = 1,
            }),
            NullLogger<UserLoginEventCleanupHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await repository.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, repository.DeleteCallCount);
    }

    private static ServiceProvider BuildServiceProvider(IUserLoginEventRepository repository) {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingUserLoginEventRepository(params int[] deleteCounts) : IUserLoginEventRepository {
        private readonly Queue<int> _deleteCounts = new(deleteCounts);
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

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

        public Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            BatchSizes.Add(batchSize);
            Cutoffs.Add(olderThanUtc);
            int deleteCount = _deleteCounts.Count == 0 ? 0 : _deleteCounts.Dequeue();
            DeletedCounts.Add(deleteCount);
            if (_deleteCounts.Count == 0) {
                _completion.TrySetResult();
            }

            return Task.FromResult(deleteCount);
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(_completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(_completion.Task, finished);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingUserLoginEventRepository : IUserLoginEventRepository {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DeleteCallCount { get; private set; }

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

        public async Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            completion.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(completion.Task, finished);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingUserLoginEventRepository : IUserLoginEventRepository {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DeleteCallCount { get; private set; }

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

        public Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            completion.TrySetResult();
            throw new InvalidOperationException("cleanup failed");
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(completion.Task, finished);
        }
    }
}
