using FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;
using FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;
using FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;
using FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Tests.Notifications;

public partial class NotificationsFeatureTests {

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
            CreateWebPushSubscriptionReadService(repository),
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
            CreateWebPushSubscriptionReadService(new InMemoryWebPushSubscriptionRepository()),
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
            CreateWebPushSubscriptionReadService(new InMemoryWebPushSubscriptionRepository()),
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
}
