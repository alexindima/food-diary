using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;
using FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;
using FoodDiary.Application.Notifications.Queries.GetNotifications;
using FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;
using FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Tests.Notifications;

[ExcludeFromCodeCoverage]
public class NotificationsFeatureTests {
    private static User CreateUser(UserId? id = null, string email = "notifications@example.com") {
        var user = User.Create(email, "hash");
        if (id is not null) {
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        }

        return user;
    }

    private static User CreateDeletedUser(UserId id) {
        User user = CreateUser(id, "deleted-notifications@example.com");
        user.DeleteAccount(DateTime.UtcNow);
        return user;
    }

    private static IUnitOfWork CreateUnitOfWork() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return unitOfWork;
    }

    private static RecordingPostCommitActionQueue CreatePostCommitActionQueue() => new();

    [Fact]
    public async Task MarkNotificationRead_WithValidOwnership_Succeeds() {
        var userId = UserId.New();
        var notification = Notification.Create(userId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);
        var pusher = new RecordingNotificationPusher();

        RecordingPostCommitActionQueue postCommitActionQueue = CreatePostCommitActionQueue();
        var handler = new MarkNotificationReadCommandHandler(repo, CreateCurrentUserAccessService(CreateUser(userId)), pusher, postCommitActionQueue);
        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, notification.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(notification.IsRead);
        Assert.Null(pusher.UnreadCountUserId);
        Assert.True(postCommitActionQueue.HasActions);

        await postCommitActionQueue.FlushAsync();

        Assert.Equal(userId.Value, pusher.UnreadCountUserId);
        Assert.Equal(0, pusher.UnreadCount);
        Assert.Equal(userId.Value, pusher.NotificationsChangedUserId);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenNotOwned_ReturnsFailure() {
        var ownerId = UserId.New();
        var otherUserId = UserId.New();
        var notification = Notification.Create(ownerId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);

        var handler = new MarkNotificationReadCommandHandler(repo, CreateCurrentUserAccessService(CreateUser(otherUserId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());
        Result result = await handler.Handle(
            new MarkNotificationReadCommand(otherUserId.Value, notification.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenNotFound_ReturnsFailure() {
        var repo = new InMemoryNotificationRepository();
        var userId = UserId.New();
        var handler = new MarkNotificationReadCommandHandler(repo, CreateCurrentUserAccessService(CreateUser(userId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkNotificationReadCommandHandler(
            new InMemoryNotificationRepository(),
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingNotificationPusher(),
            CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var notification = Notification.Create(userId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);
        var handler = new MarkNotificationReadCommandHandler(repo, CreateCurrentUserAccessService(CreateDeletedUser(userId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, notification.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WithValidUserId_Succeeds() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        repo.Seed(Notification.Create(userId, "info", "{}"));
        var pusher = new RecordingNotificationPusher();
        RecordingPostCommitActionQueue postCommitActionQueue = CreatePostCommitActionQueue();
        var handler = new MarkAllNotificationsReadCommandHandler(repo, CreateCurrentUserAccessService(CreateUser(userId)), pusher, postCommitActionQueue);

        Result result = await handler.Handle(
            new MarkAllNotificationsReadCommand(userId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repo.MarkAllReadCalled);
        Assert.Null(pusher.UnreadCountUserId);
        Assert.True(postCommitActionQueue.HasActions);

        await postCommitActionQueue.FlushAsync();

        Assert.Equal(userId.Value, pusher.UnreadCountUserId);
        Assert.Equal(0, pusher.UnreadCount);
        Assert.Equal(userId.Value, pusher.NotificationsChangedUserId);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkAllNotificationsReadCommandHandler(
            new InMemoryNotificationRepository(),
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingNotificationPusher(),
            CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkAllNotificationsReadCommand(UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        var handler = new MarkAllNotificationsReadCommandHandler(repo, CreateCurrentUserAccessService(CreateDeletedUser(userId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkAllNotificationsReadCommand(userId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repo.MarkAllReadCalled);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCount() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        repo.Seed(Notification.Create(userId, "info", "{}"));

        var handler = new GetUnreadCountQueryHandler(repo, CreateNotificationUserContextService(CreateUser(userId)));
        Result<int> result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task GetUnreadCount_WithNullUserId_ReturnsInvalidToken() {
        var handler = new GetUnreadCountQueryHandler(
            new InMemoryNotificationRepository(),
            CreateNotificationUserContextService(CreateUser()));

        Result<int> result = await handler.Handle(new GetUnreadCountQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetUnreadCount_WithPasswordUser_ExcludesPasswordSetupSuggestion() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, NotificationTypes.PasswordSetupSuggested, "{}"));
        repo.Seed(Notification.Create(userId, NotificationTypes.FastingCompleted, "{}"));
        var handler = new GetUnreadCountQueryHandler(repo, CreateNotificationUserContextService(user));

        Result<int> result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task GetUnreadCount_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        var handler = new GetUnreadCountQueryHandler(repo, CreateNotificationUserContextService(CreateDeletedUser(userId)));

        Result<int> result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_UpdatesUserAndWritesAuditLog() {
        User user = CreateUser(email: "notifications@example.com");
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpdateNotificationPreferencesCommandHandler(CreateNotificationPreferencesService(user), auditLogger);

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
        var handler = new UpdateNotificationPreferencesCommandHandler(CreateNotificationPreferencesService(user), auditLogger);

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
            new RecordingAuditLogger());

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
            auditLogger);

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
        var handler = new UpdateNotificationPreferencesCommandHandler(preferencesService, auditLogger);

        Result<NotificationPreferencesModel> result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(user.Id.Value, PushNotificationsEnabled: true, FastingPushNotificationsEnabled: null, SocialPushNotificationsEnabled: null, FastingCheckInReminderHours: null, FastingCheckInFollowUpReminderHours: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Equal(string.Empty, auditLogger.Action);
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
    public async Task GetNotificationPreferences_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(email: "deleted-notifications@example.com");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetNotificationPreferencesQueryHandler(CreateNotificationPreferencesService(user));

        Result<NotificationPreferencesModel> result = await handler.Handle(new GetNotificationPreferencesQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetNotificationPreferences_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetNotificationPreferencesQueryHandler(CreateNotificationPreferencesService(CreateUser()));

        Result<NotificationPreferencesModel> result = await handler.Handle(new GetNotificationPreferencesQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetNotificationsQueryHandler(
            new InMemoryNotificationRepository(),
            CreateNotificationUserContextService(CreateUser()),
            new RecordingNotificationTextRenderer());

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WhenUserDeleted_ReturnsAccessFailure() {
        var userId = UserId.New();
        var handler = new GetNotificationsQueryHandler(
            new InMemoryNotificationRepository(),
            CreateNotificationUserContextService(CreateDeletedUser(userId)),
            new RecordingNotificationTextRenderer());

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WithPasswordUser_HidesPasswordSetupSuggestion() {
        var user = User.Create("password-notifications@example.com", "hash");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(user.Id, NotificationTypes.PasswordSetupSuggested, "{}"));
        repo.Seed(Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}"));
        var renderer = new RecordingNotificationTextRenderer();
        var handler = new GetNotificationsQueryHandler(repo, CreateNotificationUserContextService(user), renderer);

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value);
        Assert.Equal(NotificationTypes.FastingCompleted, result.Value[0].Type);
        Assert.Equal([NotificationTypes.FastingCompleted], renderer.RenderedTypes);
    }

    [Fact]
    public async Task GetWebPushSubscriptions_FiltersExpiredSubscriptions() {
        User user = CreateUser();
        var utcNow = new DateTime(2026, 5, 28, 8, 0, 0, DateTimeKind.Utc);
        var active = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/active",
            "p256",
            "auth",
            utcNow.AddMinutes(5));
        var expired = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/expired",
            "p256",
            "auth",
            utcNow.AddMinutes(-1));
        var repository = new InMemoryWebPushSubscriptionRepository([active, expired]);
        var handler = new GetWebPushSubscriptionsQueryHandler(
            repository,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider(utcNow));

        Result<IReadOnlyList<WebPushSubscriptionModel>> result = await handler.Handle(new GetWebPushSubscriptionsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        WebPushSubscriptionModel item = Assert.Single(result.Value);
        Assert.Equal(active.Endpoint, item.Endpoint);
        Assert.Empty(repository.DeletedEndpoints);
        Assert.Equal(2, repository.Subscriptions.Count);
    }

    [Fact]
    public async Task GetWebPushSubscriptions_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWebPushSubscriptionsQueryHandler(
            new InMemoryWebPushSubscriptionRepository(),
            CreateCurrentUserAccessService(CreateUser()),
            new FixedDateTimeProvider(DateTime.UtcNow));

        Result<IReadOnlyList<WebPushSubscriptionModel>> result = await handler.Handle(new GetWebPushSubscriptionsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWebPushSubscriptions_WhenUserDeleted_ReturnsAccessFailure() {
        var userId = UserId.New();
        var handler = new GetWebPushSubscriptionsQueryHandler(
            new InMemoryWebPushSubscriptionRepository(),
            CreateCurrentUserAccessService(CreateDeletedUser(userId)),
            new FixedDateTimeProvider(DateTime.UtcNow));

        Result<IReadOnlyList<WebPushSubscriptionModel>> result = await handler.Handle(new GetWebPushSubscriptionsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetWebPushConfiguration_ReturnsClientConfiguration() {
        var handler = new GetWebPushConfigurationQueryHandler(
            new StaticWebPushConfigurationProvider(new WebPushClientConfiguration(Enabled: true, "public-key")));

        Result<WebPushConfigurationModel> result = await handler.Handle(new GetWebPushConfigurationQuery(), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.Enabled);
        Assert.Equal("public-key", result.Value.PublicKey);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithNewEndpoint_AddsSubscriptionAndWritesAuditLog() {
        User user = CreateUser();
        var repository = new InMemoryWebPushSubscriptionRepository();
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpsertWebPushSubscriptionCommandHandler(repository, CreateCurrentUserAccessService(user), auditLogger);

        Result result = await handler.Handle(
            new UpsertWebPushSubscriptionCommand(
                user.Id.Value,
                "https://push.example.com/new",
                "p256",
                "auth",
                ExpirationTimeUtc: null,
                "en",
                "Chrome"),
            CancellationToken.None);

        ResultAssert.Success(result);
        WebPushSubscription subscription = Assert.Single(repository.Subscriptions);
        Assert.Equal(user.Id, subscription.UserId);
        Assert.Equal("notifications.push-subscription.connected", auditLogger.Action);
        Assert.Contains("endpointHost=push.example.com", auditLogger.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithDeletedUser_ReturnsFailureWithoutAddingSubscription() {
        var userId = UserId.New();
        var repository = new InMemoryWebPushSubscriptionRepository();
        var handler = new UpsertWebPushSubscriptionCommandHandler(
            repository,
            CreateCurrentUserAccessService(CreateDeletedUser(userId)),
            new RecordingAuditLogger());

        Result result = await handler.Handle(
            new UpsertWebPushSubscriptionCommand(
                userId.Value,
                "https://push.example.com/new",
                "p256",
                "auth",
                ExpirationTimeUtc: null,
                "en",
                "Chrome"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Empty(repository.Subscriptions);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryWebPushSubscriptionRepository();
        var handler = new UpsertWebPushSubscriptionCommandHandler(
            repository,
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingAuditLogger());

        Result result = await handler.Handle(
            new UpsertWebPushSubscriptionCommand(
                Guid.Empty,
                "https://push.example.com/new",
                "p256",
                "auth",
                ExpirationTimeUtc: null,
                "en",
                "Chrome"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Empty(repository.Subscriptions);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithExistingEndpoint_RefreshesSubscriptionAndWritesAuditLog() {
        User user = CreateUser();
        var subscription = WebPushSubscription.Create(
            UserId.New(),
            "https://push.example.com/existing",
            "old-p256",
            "old-auth",
            expirationTimeUtc: null,
            "ru",
            "OldBrowser");
        var repository = new InMemoryWebPushSubscriptionRepository([subscription]);
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpsertWebPushSubscriptionCommandHandler(repository, CreateCurrentUserAccessService(user), auditLogger);

        Result result = await handler.Handle(
            new UpsertWebPushSubscriptionCommand(
                user.Id.Value,
                subscription.Endpoint,
                "new-p256",
                "new-auth",
                DateTime.UtcNow.AddDays(1),
                "en",
                "Chrome"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(user.Id, subscription.UserId);
        Assert.Equal("new-p256", subscription.P256Dh);
        Assert.Equal("new-auth", subscription.Auth);
        Assert.Equal("en", subscription.Locale);
        Assert.Equal("Chrome", subscription.UserAgent);
        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Equal("notifications.push-subscription.refreshed", auditLogger.Action);
        Assert.Contains("endpointHost=push.example.com", auditLogger.Details, StringComparison.Ordinal);
        Assert.Contains("locale=en", auditLogger.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_WhenOwned_DeletesSubscriptionAndWritesAuditLog() {
        User user = CreateUser();
        var subscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/remove",
            "p256",
            "auth");
        var repository = new InMemoryWebPushSubscriptionRepository([subscription]);
        var auditLogger = new RecordingAuditLogger();
        var handler = new RemoveWebPushSubscriptionCommandHandler(repository, CreateCurrentUserAccessService(user), auditLogger);

        Result result = await handler.Handle(
            new RemoveWebPushSubscriptionCommand(user.Id.Value, subscription.Endpoint),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(repository.Subscriptions);
        Assert.Equal("notifications.push-subscription.disconnected", auditLogger.Action);
        Assert.Contains("endpointHost=push.example.com", auditLogger.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryWebPushSubscriptionRepository();
        var handler = new RemoveWebPushSubscriptionCommandHandler(
            repository,
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingAuditLogger());

        Result result = await handler.Handle(
            new RemoveWebPushSubscriptionCommand(Guid.Empty, "https://push.example.com/remove"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Empty(repository.DeletedEndpoints);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_WhenUserDeleted_ReturnsAccessFailure() {
        var userId = UserId.New();
        var repository = new InMemoryWebPushSubscriptionRepository();
        var handler = new RemoveWebPushSubscriptionCommandHandler(
            repository,
            CreateCurrentUserAccessService(CreateDeletedUser(userId)),
            new RecordingAuditLogger());

        Result result = await handler.Handle(
            new RemoveWebPushSubscriptionCommand(userId.Value, "https://push.example.com/remove"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Empty(repository.DeletedEndpoints);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_WithBlankEndpoint_ReturnsSuccessWithoutLookup() {
        User user = CreateUser();
        var repository = new InMemoryWebPushSubscriptionRepository();
        var auditLogger = new RecordingAuditLogger();
        var handler = new RemoveWebPushSubscriptionCommandHandler(repository, CreateCurrentUserAccessService(user), auditLogger);

        Result result = await handler.Handle(
            new RemoveWebPushSubscriptionCommand(user.Id.Value, "   "),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, repository.EndpointLookupCount);
        Assert.Equal(string.Empty, auditLogger.Action);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_WhenEndpointMissing_ReturnsSuccessWithoutDeleting() {
        User user = CreateUser();
        var repository = new InMemoryWebPushSubscriptionRepository();
        var handler = new RemoveWebPushSubscriptionCommandHandler(
            repository,
            CreateCurrentUserAccessService(user),
            new RecordingAuditLogger());

        Result result = await handler.Handle(
            new RemoveWebPushSubscriptionCommand(user.Id.Value, "https://push.example.com/missing"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(repository.DeletedEndpoints);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_WhenEndpointBelongsToAnotherUser_ReturnsSuccessWithoutDeleting() {
        User user = CreateUser();
        var subscription = WebPushSubscription.Create(
            UserId.New(),
            "https://push.example.com/other",
            "p256",
            "auth");
        var repository = new InMemoryWebPushSubscriptionRepository([subscription]);
        var handler = new RemoveWebPushSubscriptionCommandHandler(
            repository,
            CreateCurrentUserAccessService(user),
            new RecordingAuditLogger());

        Result result = await handler.Handle(
            new RemoveWebPushSubscriptionCommand(user.Id.Value, subscription.Endpoint),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(repository.Subscriptions);
        Assert.Empty(repository.DeletedEndpoints);
    }

    [Fact]
    public async Task ScheduleTestNotification_WithValidUser_SchedulesAndWritesAuditLog() {
        User user = CreateUser();
        var scheduledAtUtc = new DateTime(2026, 5, 28, 9, 0, 0, DateTimeKind.Utc);
        var scheduler = new RecordingNotificationTestScheduler(
            new ScheduledNotificationData(NotificationTypes.FastingCompleted, 15, scheduledAtUtc));
        var auditLogger = new RecordingAuditLogger();
        var handler = new ScheduleTestNotificationCommandHandler(
            scheduler,
            CreateCurrentUserAccessService(user),
            auditLogger);

        Result<ScheduledNotificationModel> result = await handler.Handle(
            new ScheduleTestNotificationCommand(user.Id.Value, 15, NotificationTypes.FastingCompleted),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(NotificationTypes.FastingCompleted, result.Value.Type);
        Assert.Equal(15, scheduler.DelaySeconds);
        Assert.Equal(user.Id.Value, scheduler.UserId);
        Assert.Equal("notifications.test.scheduled", auditLogger.Action);
        Assert.Equal(NotificationTypes.FastingCompleted, auditLogger.TargetId);
        Assert.Contains("delaySeconds=15", auditLogger.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScheduleTestNotification_WithDeletedUser_ReturnsFailureWithoutScheduling() {
        var userId = UserId.New();
        var scheduler = new RecordingNotificationTestScheduler(
            new ScheduledNotificationData(NotificationTypes.FastingCompleted, 15, DateTime.UtcNow));
        var handler = new ScheduleTestNotificationCommandHandler(
            scheduler,
            CreateCurrentUserAccessService(CreateDeletedUser(userId)),
            new RecordingAuditLogger());

        Result<ScheduledNotificationModel> result = await handler.Handle(
            new ScheduleTestNotificationCommand(userId.Value, 15, NotificationTypes.FastingCompleted),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(scheduler.WasCalled);
    }

    [Fact]
    public async Task ScheduleTestNotification_WithEmptyUserId_ReturnsInvalidToken() {
        var scheduler = new RecordingNotificationTestScheduler(
            new ScheduledNotificationData(NotificationTypes.FastingCompleted, 15, DateTime.UtcNow));
        var handler = new ScheduleTestNotificationCommandHandler(
            scheduler,
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingAuditLogger());

        Result<ScheduledNotificationModel> result = await handler.Handle(
            new ScheduleTestNotificationCommand(Guid.Empty, 15, NotificationTypes.FastingCompleted),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(scheduler.WasCalled);
    }

    [Fact]
    public async Task NotificationCleanup_WithNonPositiveBatchSize_DoesNotCallRepository() {
        var repository = new InMemoryNotificationRepository();
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new NotificationCleanupService(
            repository,
            new FixedDateTimeProvider(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc)),
            unitOfWork);

        int deleted = await service.CleanupExpiredNotificationsAsync(
            new NotificationCleanupPolicy(["Fast"], 3, 4, 30, 60, 0),
            CancellationToken.None);

        Assert.Equal(0, deleted);
        Assert.False(repository.DeleteExpiredBatchCalled);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotificationCleanup_UsesUtcNowRetentionCutoffsAndCancellationToken() {
        using var cts = new CancellationTokenSource();
        var repository = new InMemoryNotificationRepository { DeleteExpiredBatchResult = 7 };
        var utcNow = new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new NotificationCleanupService(repository, new FixedDateTimeProvider(utcNow), unitOfWork);

        int deleted = await service.CleanupExpiredNotificationsAsync(
            new NotificationCleanupPolicy(["Fast"], 3, 4, 30, 60, 25),
            cts.Token);

        Assert.Equal(7, deleted);
        Assert.True(repository.DeleteExpiredBatchCalled);
        Assert.Equal(["Fast"], repository.TransientTypes);
        Assert.Equal(utcNow.AddDays(-3), repository.TransientReadOlderThanUtc);
        Assert.Equal(utcNow.AddDays(-4), repository.TransientUnreadOlderThanUtc);
        Assert.Equal(utcNow.AddDays(-30), repository.StandardReadOlderThanUtc);
        Assert.Equal(utcNow.AddDays(-60), repository.StandardUnreadOlderThanUtc);
        Assert.Equal(25, repository.BatchSize);
        Assert.Equal(cts.Token, repository.DeleteExpiredBatchCancellationToken);
        await unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task NotificationWriter_WhenWebPushRequested_EnqueuesOutboxMessage() {
        var repository = new InMemoryNotificationRepository();
        var outbox = new RecordingNotificationWebPushOutbox();
        var writer = new NotificationWriter(repository, outbox);
        var notification = Notification.Create(UserId.New(), "info", "{}");

        await writer.AddAsync(notification, sendWebPush: true, CancellationToken.None);

        Assert.Same(notification, Assert.Single(repository.Notifications));
        Assert.Equal(notification.Id, Assert.Single(outbox.NotificationIds));
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryNotificationRepository : INotificationRepository {
        private readonly List<Notification> _notifications = [];
        public IReadOnlyList<Notification> Notifications => _notifications;
        public bool MarkAllReadCalled { get; private set; }
        public bool DeleteExpiredBatchCalled { get; private set; }
        public IReadOnlyCollection<string> TransientTypes { get; private set; } = [];
        public DateTime TransientReadOlderThanUtc { get; private set; }
        public DateTime TransientUnreadOlderThanUtc { get; private set; }
        public DateTime StandardReadOlderThanUtc { get; private set; }
        public DateTime StandardUnreadOlderThanUtc { get; private set; }
        public int BatchSize { get; private set; }
        public int DeleteExpiredBatchResult { get; init; }
        public CancellationToken DeleteExpiredBatchCancellationToken { get; private set; }

        public void Seed(Notification notification) => _notifications.Add(notification);

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_notifications.FirstOrDefault(n => n.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default) {
            _notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Any(n => n.UserId == userId && string.Equals(n.Type, type, StringComparison.Ordinal) && string.Equals(n.ReferenceId, referenceId, StringComparison.Ordinal)));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead && string.Equals(n.Type, type, StringComparison.Ordinal)));

        public Task MarkAllReadAsync(UserId userId, CancellationToken ct = default) {
            MarkAllReadCalled = true;
            foreach (Notification? notification in _notifications.Where(n => n.UserId == userId)) {
                notification.MarkAsRead();
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken ct = default) {
            var items = _notifications.Where(n => n.UserId == userId).Take(limit).ToList();
            return Task.FromResult<IReadOnlyList<Notification>>(items);
        }

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            DeleteExpiredBatchCalled = true;
            TransientTypes = transientTypes;
            TransientReadOlderThanUtc = transientReadOlderThanUtc;
            TransientUnreadOlderThanUtc = transientUnreadOlderThanUtc;
            StandardReadOlderThanUtc = standardReadOlderThanUtc;
            StandardUnreadOlderThanUtc = standardUnreadOlderThanUtc;
            BatchSize = batchSize;
            DeleteExpiredBatchCancellationToken = cancellationToken;
            return Task.FromResult(DeleteExpiredBatchResult);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationWebPushOutbox : INotificationWebPushOutbox {
        public List<NotificationId> NotificationIds { get; } = [];

        public Task EnqueueAsync(NotificationId notificationId, CancellationToken cancellationToken = default) {
            NotificationIds.Add(notificationId);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationPusher : INotificationPusher {
        public Guid? UnreadCountUserId { get; private set; }
        public int? UnreadCount { get; private set; }
        public Guid? NotificationsChangedUserId { get; private set; }

        public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
            UnreadCountUserId = userId;
            UnreadCount = count;
            return Task.CompletedTask;
        }

        public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) {
            NotificationsChangedUserId = userId;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingPostCommitActionQueue : IPostCommitActionQueue {
        private readonly List<Func<CancellationToken, Task>> actions = [];

        public bool HasActions => actions.Count > 0;

        public void Enqueue(Func<CancellationToken, Task> action) => actions.Add(action);

        public async Task FlushAsync(CancellationToken cancellationToken = default) {
            Func<CancellationToken, Task>[] pendingActions = [.. actions];
            actions.Clear();

            foreach (Func<CancellationToken, Task> action in pendingActions) {
                await action(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWebPushSubscriptionRepository(
        IReadOnlyList<WebPushSubscription>? subscriptions = null)
        : IWebPushSubscriptionRepository {
        private readonly List<WebPushSubscription> _subscriptions = subscriptions?.ToList() ?? [];

        public IReadOnlyList<WebPushSubscription> Subscriptions => _subscriptions;
        public List<string> DeletedEndpoints { get; } = [];
        public int EndpointLookupCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<WebPushSubscription?> GetByEndpointAsync(
            string endpoint,
            bool asTracking = false,
            CancellationToken cancellationToken = default) {
            EndpointLookupCount++;
            return Task.FromResult<WebPushSubscription?>(_subscriptions.FirstOrDefault(subscription => string.Equals(subscription.Endpoint, endpoint, StringComparison.Ordinal)));
        }

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscription>>(
                _subscriptions.Where(subscription => subscription.UserId == userId).ToList());

        public Task<WebPushSubscription> AddAsync(
            WebPushSubscription subscription,
            CancellationToken cancellationToken = default) {
            _subscriptions.Add(subscription);
            return Task.FromResult(subscription);
        }

        public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
            DeletedEndpoints.Add(subscription.Endpoint);
            _subscriptions.Remove(subscription);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(
            IReadOnlyCollection<WebPushSubscription> subscriptionsToDelete,
            CancellationToken cancellationToken = default) {
            foreach (WebPushSubscription subscription in subscriptionsToDelete) {
                DeletedEndpoints.Add(subscription.Endpoint);
                _subscriptions.Remove(subscription);
            }

            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationTestScheduler(ScheduledNotificationData scheduled) : INotificationTestScheduler {
        public bool WasCalled { get; private set; }
        public Guid UserId { get; private set; }
        public int DelaySeconds { get; private set; }
        public string Type { get; private set; } = string.Empty;

        public Task<ScheduledNotificationData> ScheduleAsync(
            Guid userId,
            int delaySeconds,
            string type,
            CancellationToken cancellationToken) {
            WasCalled = true;
            UserId = userId;
            DelaySeconds = delaySeconds;
            Type = type;
            return Task.FromResult(scheduled);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StaticWebPushConfigurationProvider(WebPushClientConfiguration configuration) : IWebPushConfigurationProvider {
        public WebPushClientConfiguration GetClientConfiguration() => configuration;
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        Error? error = user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id ? error : Errors.Authentication.InvalidToken);
            });
        return service;
    }

    private static INotificationPreferencesService CreateNotificationPreferencesService(User user) =>
        new NotificationPreferencesService(new SingleUserRepository(user));

    private static INotificationUserContextService CreateNotificationUserContextService(User user) =>
        new NotificationUserContextService(new SingleUserRepository(user));

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User user) : IUserRepository, INotificationUserAccessService {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? foundUser = user.Id == userId ? user : null;
            if (foundUser is null) {
                return Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken));
            }

            if (foundUser.DeletedAt is not null) {
                return Task.FromResult(Result.Failure<User>(Errors.Authentication.AccountDeleted));
            }

            return Task.FromResult(Result.Success(foundUser));
        }

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateUserAsync(User userToUpdate, CancellationToken cancellationToken) =>
            UpdateAsync(userToUpdate, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAuditLogger : IAuditLogger {
        public string Action { get; private set; } = string.Empty;
        public UserId ActorId { get; private set; } = UserId.Empty;
        public string? TargetId { get; private set; }
        public string? Details { get; private set; }

        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) {
            Action = action;
            ActorId = actorId;
            TargetId = targetId;
            Details = details;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationTextRenderer : INotificationTextRenderer {
        public List<string> RenderedTypes { get; } = [];

        public NotificationText Render(string type, string? locale = null, params object[] arguments) =>
            new(type, Body: null);

        public NotificationText RenderFromPayload(string type, string payloadJson, string? locale = null) {
            RenderedTypes.Add(type);
            return new NotificationText(type, payloadJson);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
