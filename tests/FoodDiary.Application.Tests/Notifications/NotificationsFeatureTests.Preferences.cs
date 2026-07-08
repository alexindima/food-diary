using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Tests.Notifications;

public partial class NotificationsFeatureTests {

    [Fact]
    public async Task UpdateNotificationPreferences_UpdatesUserAndWritesAuditLog() {
        User user = CreateUser(email: "notifications@example.com");
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpdateNotificationPreferencesCommandHandler(CreateNotificationPreferencesService(user), auditLogger, CreateNotificationUserAccessService(user));

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(user.Id.Value, PushNotificationsEnabled: true, FastingPushNotificationsEnabled: false, SocialPushNotificationsEnabled: true, 12, 20),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.PushNotificationsEnabled);
        Assert.False(user.FastingPushNotificationsEnabled);
        Assert.True(user.SocialPushNotificationsEnabled);
        Assert.Equal(12, user.FastingCheckInReminderHours);
        Assert.Equal(20, user.FastingCheckInFollowUpReminderHours);
        Assert.Equal("notifications.preferences.updated", auditLogger.Action);
        Assert.Equal(user.Id, auditLogger.ActorId);
        Assert.Contains("push=True", auditLogger.Details, StringComparison.Ordinal);
        Assert.Contains("fasting=False", auditLogger.Details, StringComparison.Ordinal);
        Assert.Contains("social=True", auditLogger.Details, StringComparison.Ordinal);
        Assert.Contains("fastingReminder=12", auditLogger.Details, StringComparison.Ordinal);
        Assert.Contains("fastingReminderFollowUp=20", auditLogger.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WhenPartialReminderUpdateWouldInvertOrder_ReturnsValidationFailure() {
        User user = CreateUser(email: "partial-reminders@example.com");
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpdateNotificationPreferencesCommandHandler(CreateNotificationPreferencesService(user), auditLogger, CreateNotificationUserAccessService(user));

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(user.Id.Value, PushNotificationsEnabled: null, FastingPushNotificationsEnabled: null, SocialPushNotificationsEnabled: null, 20, FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal(12, user.FastingCheckInReminderHours);
        Assert.Equal(20, user.FastingCheckInFollowUpReminderHours);
        Assert.Equal(string.Empty, auditLogger.Action);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpdateNotificationPreferencesCommandHandler(
            CreateNotificationPreferencesService(CreateUser()),
            new RecordingAuditLogger(),
            CreateNotificationUserAccessService(CreateUser()));

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(Guid.Empty, PushNotificationsEnabled: true, FastingPushNotificationsEnabled: null, SocialPushNotificationsEnabled: null, FastingCheckInReminderHours: null, FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpdateNotificationPreferencesCommandHandler(
            CreateNotificationPreferencesService(CreateDeletedUser(userId)),
            auditLogger,
            CreateNotificationUserAccessService(CreateDeletedUser(userId)));

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(userId.Value, PushNotificationsEnabled: true, FastingPushNotificationsEnabled: null, SocialPushNotificationsEnabled: null, FastingCheckInReminderHours: null, FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(string.Empty, auditLogger.Action);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WhenUpdateServiceFails_ReturnsFailureWithoutAuditLog() {
        User user = CreateUser(email: "update-preferences-failure@example.com");
        Error error = Errors.Validation.Invalid("Notifications", "Could not update preferences.");
        INotificationPreferencesService preferencesService = Substitute.For<INotificationPreferencesService>();
        preferencesService
            .GetAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new NotificationPreferencesModel(
                PushNotificationsEnabled: false,
                FastingPushNotificationsEnabled: true,
                SocialPushNotificationsEnabled: false,
                FastingCheckInReminderHours: 12,
                FastingCheckInFollowUpReminderHours: 20))));
        preferencesService
            .UpdateAsync(user.Id, Arg.Any<UserPreferenceUpdate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<NotificationPreferencesUpdateResult>(error)));
        RecordingAuditLogger auditLogger = new();
        var handler = new UpdateNotificationPreferencesCommandHandler(preferencesService, auditLogger, Substitute.For<INotificationUserAccessService>());

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(user.Id.Value, PushNotificationsEnabled: true, FastingPushNotificationsEnabled: null, SocialPushNotificationsEnabled: null, FastingCheckInReminderHours: null, FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Equal(string.Empty, auditLogger.Action);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WhenCurrentPreferencesFail_ReturnsFailureWithoutAuditLog() {
        var userId = UserId.New();
        Error error = Errors.Authentication.InvalidToken;
        INotificationPreferencesService preferencesService = Substitute.For<INotificationPreferencesService>();
        preferencesService
            .GetAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<NotificationPreferencesModel>(error)));
        INotificationUserAccessService accessService = Substitute.For<INotificationUserAccessService>();
        accessService.EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Error?>(null));
        RecordingAuditLogger auditLogger = new();
        var handler = new UpdateNotificationPreferencesCommandHandler(preferencesService, auditLogger, accessService);

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(userId.Value, PushNotificationsEnabled: true, FastingPushNotificationsEnabled: null, SocialPushNotificationsEnabled: null, FastingCheckInReminderHours: null, FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Equal(string.Empty, auditLogger.Action);
        await preferencesService.DidNotReceive().UpdateAsync(Arg.Any<UserId>(), Arg.Any<UserPreferenceUpdate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotificationPreferencesService_UpdateAsync_WhenUserMissing_ReturnsAccessFailure() {
        INotificationUserAccessService userAccessService = Substitute.For<INotificationUserAccessService>();
        userAccessService
            .GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var service = new NotificationPreferencesService(userAccessService);

        Result<NotificationPreferencesUpdateResult> result = await service.UpdateAsync(
            UserId.New(),
            new UserPreferenceUpdate(
                PushNotificationsEnabled: true,
                FastingPushNotificationsEnabled: null,
                SocialPushNotificationsEnabled: null,
                FastingCheckInReminderHours: null,
                FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task NotificationPreferencesService_GetAsync_WhenUserMissing_ReturnsAccessFailure() {
        INotificationUserAccessService userAccessService = Substitute.For<INotificationUserAccessService>();
        userAccessService
            .GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var service = new NotificationPreferencesService(userAccessService);

        Result<NotificationPreferencesModel> result = await service.GetAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetNotificationPreferences_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(email: "deleted-notifications@example.com");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetNotificationPreferencesQueryHandler(CreateNotificationPreferencesService(user), CreateNotificationUserAccessService(user));

        Result<NotificationPreferencesModel> result = await handler.Handle(new GetNotificationPreferencesQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetNotificationPreferences_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetNotificationPreferencesQueryHandler(
            CreateNotificationPreferencesService(CreateUser()),
            CreateNotificationUserAccessService(CreateUser()));

        Result<NotificationPreferencesModel> result = await handler.Handle(new GetNotificationPreferencesQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }
}
