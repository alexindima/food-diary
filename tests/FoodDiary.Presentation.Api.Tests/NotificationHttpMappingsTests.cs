using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;
using FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;
using FoodDiary.Application.Notifications.Queries.GetNotifications;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationHttpMappingsTests {
    [Fact]
    public void ToNotificationsQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetNotificationsQuery query = userId.ToNotificationsQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToUnreadCountQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetUnreadCountQuery query = userId.ToUnreadCountQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToMarkReadCommand_MapsUserIdAndNotificationId() {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        MarkNotificationReadCommand command = notificationId.ToMarkReadCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(notificationId, command.NotificationId);
    }

    [Fact]
    public void ToMarkAllReadCommand_MapsUserId() {
        var userId = Guid.NewGuid();

        MarkAllNotificationsReadCommand command = userId.ToMarkAllReadCommand();

        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public void NotificationModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        DateTime createdAt = DateTime.UtcNow;
        var model = new NotificationModel(id, "NewRecommendation", "New recommendation", "Details here", "/dietologist", "ref-123", IsRead: false, createdAt);

        NotificationHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal("NewRecommendation", response.Type),
            () => Assert.Equal("New recommendation", response.Title),
            () => Assert.Equal("Details here", response.Body),
            () => Assert.Equal("/dietologist", response.TargetUrl),
            () => Assert.Equal("ref-123", response.ReferenceId),
            () => Assert.False(response.IsRead),
            () => Assert.Equal(createdAt, response.CreatedAtUtc));
    }

    [Fact]
    public void NotificationModel_ToHttpResponse_WithNullOptionalFields() {
        var model = new NotificationModel(Guid.NewGuid(), "info", "Title", Body: null, TargetUrl: null, ReferenceId: null, IsRead: true, DateTime.UtcNow);

        NotificationHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Null(response.Body),
            () => Assert.Null(response.TargetUrl),
            () => Assert.Null(response.ReferenceId),
            () => Assert.True(response.IsRead));
    }

    [Fact]
    public void ToNotificationPreferencesQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetNotificationPreferencesQuery query = userId.ToNotificationPreferencesQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void UpdateNotificationPreferencesRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new UpdateNotificationPreferencesHttpRequest(PushNotificationsEnabled: true, FastingPushNotificationsEnabled: false, SocialPushNotificationsEnabled: true, 12, 20);

        UpdateNotificationPreferencesCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(request.PushNotificationsEnabled, command.PushNotificationsEnabled),
            () => Assert.Equal(request.FastingPushNotificationsEnabled, command.FastingPushNotificationsEnabled),
            () => Assert.Equal(request.SocialPushNotificationsEnabled, command.SocialPushNotificationsEnabled),
            () => Assert.Equal(request.FastingCheckInReminderHours, command.FastingCheckInReminderHours),
            () => Assert.Equal(request.FastingCheckInFollowUpReminderHours, command.FastingCheckInFollowUpReminderHours));
    }

    [Fact]
    public void NotificationPreferencesModel_ToHttpResponse_MapsAllFields() {
        var model = new NotificationPreferencesModel(PushNotificationsEnabled: true, FastingPushNotificationsEnabled: false, SocialPushNotificationsEnabled: true, 12, 20);

        NotificationPreferencesHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.True(response.PushNotificationsEnabled),
            () => Assert.False(response.FastingPushNotificationsEnabled),
            () => Assert.True(response.SocialPushNotificationsEnabled),
            () => Assert.Equal(12, response.FastingCheckInReminderHours),
            () => Assert.Equal(20, response.FastingCheckInFollowUpReminderHours));
    }

    [Fact]
    public void WebPushCommandsAndQueries_MapAllFields() {
        var userId = Guid.NewGuid();
        DateTime expiration = DateTime.UtcNow.AddDays(7);
        UpsertWebPushSubscriptionCommand upsert = new UpsertWebPushSubscriptionHttpRequest(
            "https://push.example.com/subscriptions/123",
            expiration,
            new UpsertWebPushSubscriptionKeysHttpRequest("p256dh", "auth"),
            "ru",
            "Firefox").ToCommand(userId);
        RemoveWebPushSubscriptionCommand remove = new RemoveWebPushSubscriptionHttpRequest("https://push.example.com/subscriptions/123")
            .ToCommand(userId);
        ScheduleTestNotificationCommand scheduled = new ScheduleTestNotificationHttpRequest(30, "fasting.completed").ToCommand(userId);

        Assert.NotNull(NotificationHttpMappings.ToWebPushConfigurationQuery());
        Assert.Multiple(
            () => Assert.Equal(userId, userId.ToWebPushSubscriptionsQuery().UserId),
            () => Assert.Equal(userId, upsert.UserId),
            () => Assert.Equal("https://push.example.com/subscriptions/123", upsert.Endpoint),
            () => Assert.Equal("p256dh", upsert.P256Dh),
            () => Assert.Equal("auth", upsert.Auth),
            () => Assert.Equal(expiration, upsert.ExpirationTimeUtc),
            () => Assert.Equal("ru", upsert.Locale),
            () => Assert.Equal("Firefox", upsert.UserAgent),
            () => Assert.Equal(userId, remove.UserId),
            () => Assert.Equal("https://push.example.com/subscriptions/123", remove.Endpoint),
            () => Assert.Equal(userId, scheduled.UserId),
            () => Assert.Equal(30, scheduled.DelaySeconds),
            () => Assert.Equal("fasting.completed", scheduled.Type));
    }

    [Fact]
    public void WebPushSubscriptionModel_ToHttpResponse_MapsAllFields() {
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        DateTime updatedAtUtc = DateTime.UtcNow;
        var model = new WebPushSubscriptionModel(
            "not-a-uri",
            "not-a-uri",
            ExpirationTimeUtc: null,
            "en",
            "Chrome",
            createdAtUtc,
            updatedAtUtc);

        WebPushSubscriptionHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal("not-a-uri", response.Endpoint),
            () => Assert.Equal("not-a-uri", response.EndpointHost),
            () => Assert.Null(response.ExpirationTimeUtc),
            () => Assert.Equal("en", response.Locale),
            () => Assert.Equal("Chrome", response.UserAgent),
            () => Assert.Equal(createdAtUtc, response.CreatedAtUtc),
            () => Assert.Equal(updatedAtUtc, response.UpdatedAtUtc));
    }

    [Fact]
    public void ScheduledAndConfigurationModels_ToHttpResponse_MapAllFields() {
        DateTime scheduledAtUtc = DateTime.UtcNow.AddMinutes(1);
        var scheduled = new ScheduledNotificationModel("fasting.completed", 45, scheduledAtUtc);
        var configuration = new WebPushConfigurationModel(Enabled: true, "public-key");

        ScheduledNotificationHttpResponse scheduledResponse = scheduled.ToHttpResponse();
        WebPushConfigurationHttpResponse configurationResponse = configuration.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal("fasting.completed", scheduledResponse.Type),
            () => Assert.Equal(45, scheduledResponse.DelaySeconds),
            () => Assert.Equal(scheduledAtUtc, scheduledResponse.ScheduledAtUtc),
            () => Assert.True(configurationResponse.Enabled),
            () => Assert.Equal("public-key", configurationResponse.PublicKey));
    }
}
