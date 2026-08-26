using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Results;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationTestSchedulerTests {
    [Theory]
    [InlineData("", NotificationTypes.FastingCompleted)]
    [InlineData(" unknown ", NotificationTypes.FastingCompleted)]
    [InlineData(NotificationTypes.FastingCompleted, NotificationTypes.FastingCompleted)]
    [InlineData(NotificationTypes.FastingCheckInReminder, NotificationTypes.FastingCheckInReminder)]
    [InlineData(NotificationTypes.EatingWindowStarted, NotificationTypes.EatingWindowStarted)]
    [InlineData(NotificationTypes.FastingWindowStarted, NotificationTypes.FastingWindowStarted)]
    public async Task ScheduleAsync_NormalizesAndDispatchesCommand(string type, string expectedType) {
        var dispatched = new TaskCompletionSource<(Guid UserId, string Type)>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITestNotificationDeliveryDispatcher dispatcher = Substitute.For<ITestNotificationDeliveryDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                dispatched.TrySetResult((call.ArgAt<Guid>(0), call.ArgAt<string>(1)));
                return Task.CompletedTask;
            });
        await using ServiceProvider serviceProvider = CreateServiceProvider(dispatcher);
        NotificationTestScheduler scheduler = CreateScheduler(serviceProvider, maxPending: 10);
        await scheduler.StartAsync(CancellationToken.None);
        var userId = Guid.NewGuid();

        Result<ScheduledNotificationData> result = await scheduler.ScheduleAsync(userId, 0, type, CancellationToken.None);
        Assert.True(result.IsSuccess);
        ScheduledNotificationData scheduled = result.Value;
        (Guid commandUserId, string commandType) = await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await scheduler.StopAsync(CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(expectedType, scheduled.Type),
            () => Assert.Equal(1, scheduled.DelaySeconds),
            () => Assert.Equal(userId, commandUserId),
            () => Assert.Equal(expectedType, commandType));
    }

    [Fact]
    public async Task ScheduleAsync_WhenCapacityReached_ReturnsRateLimitedFailure() {
        await using ServiceProvider serviceProvider = CreateServiceProvider(Substitute.For<ITestNotificationDeliveryDispatcher>());
        NotificationTestScheduler scheduler = CreateScheduler(serviceProvider, maxPending: 2);

        Result<ScheduledNotificationData> first = await scheduler.ScheduleAsync(
            Guid.NewGuid(), 3600, NotificationTypes.FastingCompleted, CancellationToken.None);
        Result<ScheduledNotificationData> second = await scheduler.ScheduleAsync(
            Guid.NewGuid(), 3600, NotificationTypes.FastingCompleted, CancellationToken.None);
        Result<ScheduledNotificationData> rejected = await scheduler.ScheduleAsync(
            Guid.NewGuid(), 3600, NotificationTypes.FastingCompleted, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(rejected.IsFailure);
        Assert.Equal("Notifications.TestScheduleCapacityExceeded", rejected.Error.Code);
        Assert.Equal(ErrorKind.RateLimited, rejected.Error.Kind);
    }

    [Fact]
    public async Task ScheduleAsync_WhenCalledConcurrently_NeverExceedsCapacity() {
        const int capacity = 8;
        await using ServiceProvider serviceProvider = CreateServiceProvider(Substitute.For<ITestNotificationDeliveryDispatcher>());
        NotificationTestScheduler scheduler = CreateScheduler(serviceProvider, capacity);

        Result<ScheduledNotificationData>[] results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ =>
            scheduler.ScheduleAsync(Guid.NewGuid(), 3600, NotificationTypes.FastingCompleted, CancellationToken.None)));

        Assert.Multiple(
            () => Assert.Equal(capacity, results.Count(static result => result.IsSuccess)),
            () => Assert.Equal(64 - capacity, results.Count(static result => result.IsFailure)),
            () => Assert.All(results.Where(static result => result.IsFailure), result =>
                Assert.Equal("Notifications.TestScheduleCapacityExceeded", result.Error.Code)));
    }

    [Fact]
    public async Task ScheduleAsync_WhenCallerCancelled_Throws() {
        await using ServiceProvider serviceProvider = CreateServiceProvider(Substitute.For<ITestNotificationDeliveryDispatcher>());
        NotificationTestScheduler scheduler = CreateScheduler(serviceProvider, maxPending: 1);

        await Assert.ThrowsAsync<OperationCanceledException>(() => scheduler.ScheduleAsync(
            Guid.NewGuid(),
            1,
            NotificationTypes.FastingCompleted,
            new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenApplicationStops_DoesNotDispatchPendingItems() {
        ITestNotificationDeliveryDispatcher dispatcher = Substitute.For<ITestNotificationDeliveryDispatcher>();
        await using ServiceProvider serviceProvider = CreateServiceProvider(dispatcher);
        NotificationTestScheduler scheduler = CreateScheduler(serviceProvider, maxPending: 1);
        await scheduler.StartAsync(CancellationToken.None);
        Result<ScheduledNotificationData> result = await scheduler.ScheduleAsync(
            Guid.NewGuid(), 3600, NotificationTypes.FastingCompleted, CancellationToken.None);

        await scheduler.StopAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        await dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default, default!, default);
    }

    private static NotificationTestScheduler CreateScheduler(ServiceProvider serviceProvider, int maxPending) =>
        new(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            MsOptions.Create(new NotificationTestSchedulerOptions { MaxPending = maxPending }),
            NullLogger<NotificationTestScheduler>.Instance);

    private static ServiceProvider CreateServiceProvider(ITestNotificationDeliveryDispatcher dispatcher) =>
        new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider();
}
