using FoodDiary.Application.Fasting.Services;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class FastingNotificationHostedServiceTests {
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotProcessNotifications() {
        var scheduler = new RecordingFastingNotificationScheduler();
        await using ServiceProvider provider = BuildServiceProvider(scheduler);
        var service = new FastingNotificationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new FastingNotificationOptions { Enabled = false, PollIntervalSeconds = 1 }),
            NullLogger<FastingNotificationHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, scheduler.CallCount);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_ProcessesNotifications() {
        var scheduler = new RecordingFastingNotificationScheduler();
        await using ServiceProvider provider = BuildServiceProvider(scheduler);
        var service = new FastingNotificationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new FastingNotificationOptions { Enabled = true, PollIntervalSeconds = 1 }),
            NullLogger<FastingNotificationHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await scheduler.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.True(scheduler.CallCount >= 1);
    }

    [Fact]
    public async Task StartAsync_WhenSchedulerThrows_ContinuesUntilStopped() {
        var scheduler = new ThrowingFastingNotificationScheduler();
        await using ServiceProvider provider = BuildServiceProvider(scheduler);
        var service = new FastingNotificationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new FastingNotificationOptions { Enabled = true, PollIntervalSeconds = 1 }),
            NullLogger<FastingNotificationHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await scheduler.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, scheduler.CallCount);
    }

    [Fact]
    public async Task StartAsync_WhenSchedulerObservesCancellation_StopsCleanly() {
        var scheduler = new CancelingFastingNotificationScheduler();
        await using ServiceProvider provider = BuildServiceProvider(scheduler);
        var service = new FastingNotificationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new FastingNotificationOptions { Enabled = true, PollIntervalSeconds = 1 }),
            NullLogger<FastingNotificationHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await scheduler.WaitAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, scheduler.CallCount);
    }

    private static ServiceProvider BuildServiceProvider(IFastingNotificationScheduler scheduler) {
        var services = new ServiceCollection();
        services.AddSingleton(scheduler);
        services.AddSingleton<IFastingNotificationScheduler>(scheduler);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingFastingNotificationScheduler : IFastingNotificationScheduler {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
            CallCount++;
            completion.TrySetResult();
            return Task.FromResult(0);
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(completion.Task, finished);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingFastingNotificationScheduler : IFastingNotificationScheduler {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public async Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
            CallCount++;
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
    private sealed class ThrowingFastingNotificationScheduler : IFastingNotificationScheduler {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
            CallCount++;
            completion.TrySetResult();
            throw new InvalidOperationException("scheduler failed");
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(completion.Task, finished);
        }
    }
}
