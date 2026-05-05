using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class UserLoginEventCleanupHostedServiceTests {
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotDeleteLoginEvents() {
        var repository = new RecordingUserLoginEventRepository();
        using var provider = BuildServiceProvider(repository);
        var service = new UserLoginEventCleanupHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new UserLoginEventCleanupOptions {
                Enabled = false,
                RetentionDays = 30,
                BatchSize = 10,
                PollIntervalHours = 1
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
        using var provider = BuildServiceProvider(repository);
        var service = new UserLoginEventCleanupHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new UserLoginEventCleanupOptions {
                Enabled = true,
                RetentionDays = 30,
                BatchSize = 10,
                PollIntervalHours = 1
            }),
            NullLogger<UserLoginEventCleanupHostedService>.Instance);
        var beforeStartUtc = DateTime.UtcNow;

        await service.StartAsync(CancellationToken.None);
        await repository.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        var afterStartUtc = DateTime.UtcNow;
        Assert.Equal(3, repository.DeleteCallCount);
        Assert.Equal([10, 10, 10], repository.BatchSizes);
        Assert.Equal([10, 10, 3], repository.DeletedCounts);
        Assert.All(repository.Cutoffs, cutoff => {
            Assert.True(cutoff >= beforeStartUtc.AddDays(-30).AddSeconds(-1));
            Assert.True(cutoff <= afterStartUtc.AddDays(-30).AddSeconds(1));
        });
    }

    private static ServiceProvider BuildServiceProvider(IUserLoginEventRepository repository) {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        return services.BuildServiceProvider();
    }

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
            var deleteCount = _deleteCounts.Count == 0 ? 0 : _deleteCounts.Dequeue();
            DeletedCounts.Add(deleteCount);
            if (_deleteCounts.Count == 0) {
                _completion.TrySetResult();
            }

            return Task.FromResult(deleteCount);
        }

        public async Task WaitAsync() {
            var finished = await Task.WhenAny(_completion.Task, Task.Delay(TimeSpan.FromSeconds(3)));
            Assert.Same(_completion.Task, finished);
        }
    }
}
