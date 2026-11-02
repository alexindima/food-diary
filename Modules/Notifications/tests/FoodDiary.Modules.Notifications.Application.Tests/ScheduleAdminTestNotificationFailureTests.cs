using FoodDiary.Audit.Contracts.Common.Abstractions.Audit;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ScheduleAdminTestNotificationFailureTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyActorOrTarget_DoesNotReadProfilesScheduleOrAudit(bool emptyActor) {
        IUserNotificationProfileService profiles = Substitute.For<IUserNotificationProfileService>();
        INotificationTestScheduler scheduler = Substitute.For<INotificationTestScheduler>();
        IAuditLogger audit = Substitute.For<IAuditLogger>();
        var handler = new ScheduleAdminTestNotificationCommandHandler(profiles, scheduler, audit);

        Result<ScheduledNotificationModel> result = await handler.Handle(new ScheduleAdminTestNotificationCommand(
            emptyActor ? Guid.Empty : Guid.NewGuid(), emptyActor ? Guid.NewGuid() : Guid.Empty, 10, "test"), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
        Assert.Empty(profiles.ReceivedCalls());
        Assert.Empty(scheduler.ReceivedCalls());
        Assert.Empty(audit.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProfileOrSchedulerFailure_DoesNotAuditASuccess(bool profileFails) {
        var target = UserId.New();
        IUserNotificationProfileService profiles = Substitute.For<IUserNotificationProfileService>();
        var failure = new Error("Notification.Unavailable", "Unavailable.");
        profiles.GetAsync(target, Arg.Any<CancellationToken>()).Returns(profileFails
            ? Result.Failure<UserNotificationProfileModel>(failure)
            : Result.Success(new UserNotificationProfileModel(target, HasPassword: true, "en", PushNotificationsEnabled: true,
                FastingPushNotificationsEnabled: true, SocialPushNotificationsEnabled: true, 6, 2)));
        INotificationTestScheduler scheduler = Substitute.For<INotificationTestScheduler>();
        scheduler.ScheduleAsync(target.Value, 10, "test", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ScheduledNotificationData>(failure));
        IAuditLogger audit = Substitute.For<IAuditLogger>();
        var handler = new ScheduleAdminTestNotificationCommandHandler(profiles, scheduler, audit);

        Result<ScheduledNotificationModel> result = await handler.Handle(
            new ScheduleAdminTestNotificationCommand(Guid.NewGuid(), target.Value, 10, "test"), CancellationToken.None);

        Error error = ResultAssert.Failure(result);
        Assert.Equal(profileFails ? "User.NotFound" : failure.Code, error.Code);
        Assert.Empty(audit.ReceivedCalls());
        if (profileFails) {
            Assert.Empty(scheduler.ReceivedCalls());
        }
    }
}
