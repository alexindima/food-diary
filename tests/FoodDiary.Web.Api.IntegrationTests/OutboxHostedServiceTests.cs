using System.Reflection;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class OutboxHostedServiceTests {
    [Fact]
    public async Task NotificationWebPushOutboxHostedService_WhenDisabled_DoesNotProcess() {
        var processor = new RecordingNotificationWebPushOutboxProcessor();
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new NotificationWebPushOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new NotificationWebPushOutboxOptions { Enabled = false }),
            NullLogger<NotificationWebPushOutboxHostedService>.Instance);

        await InvokeExecuteAsync(service, CancellationToken.None);

        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task NotificationWebPushOutboxHostedService_WhenEnabled_ProcessesImmediatelyAndOnTimer() {
        var processor = new RecordingNotificationWebPushOutboxProcessor(processedCount: 1);
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new NotificationWebPushOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new NotificationWebPushOutboxOptions {
                Enabled = true,
                BatchSize = 7,
                PollIntervalSeconds = 1,
            }),
            NullLogger<NotificationWebPushOutboxHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await processor.WaitForCallCountAsync(2);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal([7, 7], processor.BatchSizes);
    }

    [Fact]
    public async Task NotificationWebPushOutboxHostedService_WhenProcessorThrows_HandlesFailure() {
        var processor = new ThrowingNotificationWebPushOutboxProcessor();
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new NotificationWebPushOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new NotificationWebPushOutboxOptions { Enabled = true, BatchSize = 5, PollIntervalSeconds = 1 }),
            NullLogger<NotificationWebPushOutboxHostedService>.Instance);

        await InvokeProcessOnceAsync(service, new NotificationWebPushOutboxOptions { Enabled = true, BatchSize = 5, PollIntervalSeconds = 1 }, CancellationToken.None);

        Assert.Equal(1, processor.CallCount);
    }

    [Fact]
    public async Task NotificationWebPushOutboxHostedService_WhenCancellationIsObserved_Rethrows() {
        var processor = new CancelingNotificationWebPushOutboxProcessor();
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new NotificationWebPushOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new NotificationWebPushOutboxOptions { Enabled = true }),
            NullLogger<NotificationWebPushOutboxHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeProcessOnceAsync(service, new NotificationWebPushOutboxOptions { Enabled = true }, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ImageObjectDeletionOutboxHostedService_WhenDisabled_DoesNotProcess() {
        var processor = new RecordingImageObjectDeletionOutboxProcessor();
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new ImageObjectDeletionOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new ImageObjectDeletionOutboxOptions { Enabled = false }),
            NullLogger<ImageObjectDeletionOutboxHostedService>.Instance);

        await InvokeExecuteAsync(service, CancellationToken.None);

        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task ImageObjectDeletionOutboxHostedService_WhenEnabled_ProcessesImmediatelyAndOnTimer() {
        var processor = new RecordingImageObjectDeletionOutboxProcessor(processedCount: 1);
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new ImageObjectDeletionOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new ImageObjectDeletionOutboxOptions {
                Enabled = true,
                BatchSize = 9,
                PollIntervalSeconds = 1,
            }),
            NullLogger<ImageObjectDeletionOutboxHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await processor.WaitForCallCountAsync(2);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal([9, 9], processor.BatchSizes);
    }

    [Fact]
    public async Task ImageObjectDeletionOutboxHostedService_WhenProcessorThrows_HandlesFailure() {
        var processor = new ThrowingImageObjectDeletionOutboxProcessor();
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new ImageObjectDeletionOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new ImageObjectDeletionOutboxOptions { Enabled = true, BatchSize = 5, PollIntervalSeconds = 1 }),
            NullLogger<ImageObjectDeletionOutboxHostedService>.Instance);

        await InvokeProcessOnceAsync(service, new ImageObjectDeletionOutboxOptions { Enabled = true, BatchSize = 5, PollIntervalSeconds = 1 }, CancellationToken.None);

        Assert.Equal(1, processor.CallCount);
    }

    [Fact]
    public async Task ImageObjectDeletionOutboxHostedService_WhenCancellationIsObserved_Rethrows() {
        var processor = new CancelingImageObjectDeletionOutboxProcessor();
        await using ServiceProvider provider = BuildServiceProvider(processor);
        var service = new ImageObjectDeletionOutboxHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new ImageObjectDeletionOutboxOptions { Enabled = true }),
            NullLogger<ImageObjectDeletionOutboxHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeProcessOnceAsync(service, new ImageObjectDeletionOutboxOptions { Enabled = true }, cancellationTokenSource.Token));
    }

    private static ServiceProvider BuildServiceProvider(INotificationWebPushOutboxProcessor processor) {
        var services = new ServiceCollection();
        services.AddSingleton(processor);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildServiceProvider(IImageObjectDeletionOutboxProcessor processor) {
        var services = new ServiceCollection();
        services.AddSingleton(processor);
        return services.BuildServiceProvider();
    }

    private static async Task InvokeExecuteAsync(BackgroundService service, CancellationToken cancellationToken) {
        MethodInfo method = service.GetType().GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [cancellationToken])!).ConfigureAwait(false);
    }

    private static async Task InvokeProcessOnceAsync(
        NotificationWebPushOutboxHostedService service,
        NotificationWebPushOutboxOptions options,
        CancellationToken cancellationToken) {
        MethodInfo method = typeof(NotificationWebPushOutboxHostedService).GetMethod(
            "ProcessOnceAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [options, cancellationToken])!).ConfigureAwait(false);
    }

    private static async Task InvokeProcessOnceAsync(
        ImageObjectDeletionOutboxHostedService service,
        ImageObjectDeletionOutboxOptions options,
        CancellationToken cancellationToken) {
        MethodInfo method = typeof(ImageObjectDeletionOutboxHostedService).GetMethod(
            "ProcessOnceAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [options, cancellationToken])!).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationWebPushOutboxProcessor(int processedCount = 0) : INotificationWebPushOutboxProcessor {
        private readonly TaskCompletionSource secondCallCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }
        public List<int> BatchSizes { get; } = [];

        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
            CallCount++;
            BatchSizes.Add(batchSize);
            if (CallCount >= 2) {
                secondCallCompletion.TrySetResult();
            }

            return Task.FromResult(processedCount);
        }

        public async Task WaitForCallCountAsync(int expectedCallCount) {
            if (CallCount >= expectedCallCount) {
                return;
            }

            await AsyncTestAwaiter.WaitAsync(
                secondCallCompletion.Task,
                TimeSpan.FromSeconds(3),
                "Notification web-push outbox processor was not called enough times before the timeout.").ConfigureAwait(false);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingNotificationWebPushOutboxProcessor : INotificationWebPushOutboxProcessor {
        public int CallCount { get; private set; }

        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
            CallCount++;
            throw new InvalidOperationException("notification outbox failed");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingNotificationWebPushOutboxProcessor : INotificationWebPushOutboxProcessor {
        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromCanceled<int>(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageObjectDeletionOutboxProcessor(int processedCount = 0) : IImageObjectDeletionOutboxProcessor {
        private readonly TaskCompletionSource secondCallCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }
        public List<int> BatchSizes { get; } = [];

        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
            CallCount++;
            BatchSizes.Add(batchSize);
            if (CallCount >= 2) {
                secondCallCompletion.TrySetResult();
            }

            return Task.FromResult(processedCount);
        }

        public async Task WaitForCallCountAsync(int expectedCallCount) {
            if (CallCount >= expectedCallCount) {
                return;
            }

            await AsyncTestAwaiter.WaitAsync(
                secondCallCompletion.Task,
                TimeSpan.FromSeconds(3),
                "Image object deletion outbox processor was not called enough times before the timeout.").ConfigureAwait(false);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingImageObjectDeletionOutboxProcessor : IImageObjectDeletionOutboxProcessor {
        public int CallCount { get; private set; }

        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
            CallCount++;
            throw new InvalidOperationException("image outbox failed");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingImageObjectDeletionOutboxProcessor : IImageObjectDeletionOutboxProcessor {
        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromCanceled<int>(cancellationToken);
    }
}
