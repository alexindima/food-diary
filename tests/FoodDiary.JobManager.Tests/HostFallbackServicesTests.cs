using FoodDiary.JobManager.Services;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class HostFallbackServicesTests {
    [Fact]
    public async Task EmailVerificationNotifier_CompletesWithoutHostTransport() {
        IEmailVerificationNotifier notifier = Create<IEmailVerificationNotifier>("NoOpEmailVerificationNotifier");
        await notifier.NotifyEmailVerifiedAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task NotificationTestScheduler_ReportsUnavailableInJobHost() {
        INotificationTestScheduler scheduler = Create<INotificationTestScheduler>("UnavailableNotificationTestScheduler");
        Result<ScheduledNotificationData> result = await scheduler.ScheduleAsync(Guid.NewGuid(), 1, "test", CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Multiple(
            () => Assert.Equal("NotificationTestScheduler.Unavailable", result.Error.Code),
            () => Assert.Equal(ErrorKind.Internal, result.Error.Kind));
    }

    private static T Create<T>(string name) => Assert.IsAssignableFrom<T>(Activator.CreateInstance(
        typeof(UserCleanupJob).Assembly.GetType($"FoodDiary.JobManager.Services.{name}", throwOnError: true)!, nonPublic: true));
}
