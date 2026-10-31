using FluentValidation.TestHelper;
using FoodDiary.Audit.Contracts.Common.Abstractions.Audit;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ScheduleAdminTestNotificationTests {
    private readonly IUserNotificationProfileService _profiles = Substitute.For<IUserNotificationProfileService>();
    private readonly INotificationTestScheduler _scheduler = Substitute.For<INotificationTestScheduler>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    [Fact]
    public async Task Handle_SchedulesForTargetAndAuditsAdministratorAsync() {
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        var targetId = new UserId(_targetId);
        _profiles.GetAsync(targetId, token).Returns(Result.Success(new UserNotificationProfileModel(
            targetId, HasPassword: true, "ru", PushNotificationsEnabled: true, FastingPushNotificationsEnabled: true, SocialPushNotificationsEnabled: true, 12, 24)));
        DateTime scheduledAt = new(2026, 10, 31, 12, 0, 15, DateTimeKind.Utc);
        _scheduler.ScheduleAsync(_targetId, 15, NotificationTypes.EatingWindowStarted, token)
            .Returns(Result.Success(new ScheduledNotificationData(NotificationTypes.EatingWindowStarted, 15, scheduledAt)));

        Result<ScheduledNotificationModel> result = await CreateHandler().Handle(
            new ScheduleAdminTestNotificationCommand(_actorId, _targetId, 15, NotificationTypes.EatingWindowStarted), token);

        ResultAssert.Success(result);
        Assert.Equal(scheduledAt, result.Value.ScheduledAtUtc);
        await _scheduler.Received(1).ScheduleAsync(_targetId, 15, NotificationTypes.EatingWindowStarted, token);
        _audit.Received(1).Log("admin.notifications.test.scheduled", new UserId(_actorId), "User", _targetId.ToString(),
            Arg.Is<string>(details => details.Contains(NotificationTypes.EatingWindowStarted, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Handle_UnavailableTargetReturnsNotFoundWithoutSchedulingAsync() {
        _profiles.GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserNotificationProfileModel>(AuthenticationErrors.InvalidToken));

        Result<ScheduledNotificationModel> result = await CreateHandler().Handle(Command(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(UserErrors.NotFound(_targetId), result.Error);
        await _scheduler.DidNotReceiveWithAnyArgs().ScheduleAsync(default, default, default!, default);
        _audit.DidNotReceiveWithAnyArgs().Log(default!, default, default, default, default);
    }

    [Fact]
    public async Task Handle_SchedulerFailureIsReturnedWithoutSuccessAuditAsync() {
        var targetId = new UserId(_targetId);
        _profiles.GetAsync(targetId, Arg.Any<CancellationToken>()).Returns(Result.Success(new UserNotificationProfileModel(
            targetId, HasPassword: true, "en", PushNotificationsEnabled: true, FastingPushNotificationsEnabled: true, SocialPushNotificationsEnabled: true, 12, 24)));
        var error = new Error("Notifications.Capacity", "Queue full", Kind: ErrorKind.Conflict);
        _scheduler.ScheduleAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ScheduledNotificationData>(error));

        Result<ScheduledNotificationModel> result = await CreateHandler().Handle(Command(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        _audit.DidNotReceiveWithAnyArgs().Log(default!, default, default, default, default);
    }

    [Theory]
    [InlineData(0, "FastingCompleted")]
    [InlineData(3601, "FastingCompleted")]
    [InlineData(15, "unsupported")]
    [InlineData(15, "")]
    public async Task Validator_RejectsUnsupportedInputAsync(int delay, string type) {
        var validator = new ScheduleAdminTestNotificationCommandValidator();
        TestValidationResult<ScheduleAdminTestNotificationCommand> result = await validator.TestValidateAsync(Command() with { DelaySeconds = delay, Type = type });
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(1, NotificationTypes.FastingCompleted)]
    [InlineData(3600, NotificationTypes.FastingCheckInReminder)]
    [InlineData(15, NotificationTypes.EatingWindowStarted)]
    [InlineData(30, NotificationTypes.FastingWindowStarted)]
    public async Task Validator_AcceptsSupportedTypesAndDelayBoundariesAsync(int delay, string type) {
        var validator = new ScheduleAdminTestNotificationCommandValidator();
        TestValidationResult<ScheduleAdminTestNotificationCommand> result = await validator.TestValidateAsync(Command() with { DelaySeconds = delay, Type = type });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validator_RejectsEmptyIdentitiesAsync() {
        var validator = new ScheduleAdminTestNotificationCommandValidator();
        TestValidationResult<ScheduleAdminTestNotificationCommand> result = await validator.TestValidateAsync(Command() with { ActorUserId = Guid.Empty, TargetUserId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.ActorUserId);
        result.ShouldHaveValidationErrorFor(command => command.TargetUserId);
    }

    private ScheduleAdminTestNotificationCommand Command() => new(_actorId, _targetId, 15, NotificationTypes.FastingCompleted);

    private ScheduleAdminTestNotificationCommandHandler CreateHandler() => new(_profiles, _scheduler, _audit);
}
