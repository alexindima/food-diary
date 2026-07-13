using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

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
        IHostApplicationLifetime lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        await using ServiceProvider serviceProvider = CreateServiceProvider(dispatcher);
        var scheduler = new NotificationTestScheduler(serviceProvider.GetRequiredService<IServiceScopeFactory>(), lifetime, FixedTime, NullLogger<NotificationTestScheduler>.Instance);
        var userId = Guid.NewGuid();

        ScheduledNotificationData scheduled = await scheduler.ScheduleAsync(userId, 0, type, CancellationToken.None);
        (Guid CommandUserId, string CommandType) = await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Multiple(
            () => Assert.Equal(expectedType, scheduled.Type),
            () => Assert.Equal(1, scheduled.DelaySeconds),
            () => Assert.Equal(FixedUtcNow.AddSeconds(1), scheduled.ScheduledAtUtc),
            () => Assert.Equal(userId, CommandUserId),
            () => Assert.Equal(expectedType, CommandType));
    }

    [Fact]
    public async Task ScheduleAsync_WhenCallerCancelled_Throws() {
        await using ServiceProvider serviceProvider = CreateServiceProvider(Substitute.For<ITestNotificationDeliveryDispatcher>());
        var scheduler = new NotificationTestScheduler(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IHostApplicationLifetime>(),
            FixedTime,
            NullLogger<NotificationTestScheduler>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scheduler.ScheduleAsync(Guid.NewGuid(), 1, NotificationTypes.FastingCompleted, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task RunScheduledAsync_WhenDeliveryFails_SwallowsFailure() {
        ITestNotificationDeliveryDispatcher dispatcher = Substitute.For<ITestNotificationDeliveryDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("delivery failed"));
        await using ServiceProvider serviceProvider = CreateServiceProvider(dispatcher);
        var scheduler = new NotificationTestScheduler(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IHostApplicationLifetime>(),
            FixedTime,
            NullLogger<NotificationTestScheduler>.Instance);

        await InvokeRunScheduledAsync(scheduler, CancellationToken.None);

        await dispatcher.Received(1).DispatchAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunScheduledAsync_WhenApplicationStopping_SwallowsCancellation() {
        ITestNotificationDeliveryDispatcher dispatcher = Substitute.For<ITestNotificationDeliveryDispatcher>();
        await using ServiceProvider serviceProvider = CreateServiceProvider(dispatcher);
        var scheduler = new NotificationTestScheduler(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IHostApplicationLifetime>(),
            FixedTime,
            NullLogger<NotificationTestScheduler>.Instance);

        await InvokeRunScheduledAsync(scheduler, new CancellationToken(canceled: true));

        await dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default, default!, default);
    }

    private static Task InvokeRunScheduledAsync(NotificationTestScheduler scheduler, CancellationToken cancellationToken) {
        System.Reflection.MethodInfo method = typeof(NotificationTestScheduler).GetMethod(
            "RunScheduledAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(scheduler, [Guid.NewGuid(), 0, NotificationTypes.FastingCompleted, cancellationToken])!;
    }

    private static ServiceProvider CreateServiceProvider(ITestNotificationDeliveryDispatcher dispatcher) =>
        new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider();

    private static readonly DateTime FixedUtcNow = new(2026, 4, 10, 13, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider(FixedUtcNow);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
