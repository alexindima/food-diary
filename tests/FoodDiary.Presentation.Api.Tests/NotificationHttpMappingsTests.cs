using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationHttpMappingsTests {
    [Fact]
    public void ToNotificationsQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToNotificationsQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToUnreadCountQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToUnreadCountQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToMarkReadCommand_MapsUserIdAndNotificationId() {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        var command = notificationId.ToMarkReadCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(notificationId, command.NotificationId);
    }

    [Fact]
    public void ToMarkAllReadCommand_MapsUserId() {
        var userId = Guid.NewGuid();

        var command = userId.ToMarkAllReadCommand();

        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public void NotificationModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var model = new NotificationModel(id, "NewRecommendation", "New recommendation", "Details here", "/dietologist", "ref-123", false, createdAt);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("NewRecommendation", response.Type);
        Assert.Equal("New recommendation", response.Title);
        Assert.Equal("Details here", response.Body);
        Assert.Equal("/dietologist", response.TargetUrl);
        Assert.Equal("ref-123", response.ReferenceId);
        Assert.False(response.IsRead);
        Assert.Equal(createdAt, response.CreatedAtUtc);
    }

    [Fact]
    public void NotificationModel_ToHttpResponse_WithNullOptionalFields() {
        var model = new NotificationModel(Guid.NewGuid(), "info", "Title", null, null, null, true, DateTime.UtcNow);

        var response = model.ToHttpResponse();

        Assert.Null(response.Body);
        Assert.Null(response.TargetUrl);
        Assert.Null(response.ReferenceId);
        Assert.True(response.IsRead);
    }

    [Fact]
    public void ToNotificationPreferencesQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToNotificationPreferencesQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void UpdateNotificationPreferencesRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new UpdateNotificationPreferencesHttpRequest(true, false, true, 12, 20);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.PushNotificationsEnabled, command.PushNotificationsEnabled);
        Assert.Equal(request.FastingPushNotificationsEnabled, command.FastingPushNotificationsEnabled);
        Assert.Equal(request.SocialPushNotificationsEnabled, command.SocialPushNotificationsEnabled);
        Assert.Equal(request.FastingCheckInReminderHours, command.FastingCheckInReminderHours);
        Assert.Equal(request.FastingCheckInFollowUpReminderHours, command.FastingCheckInFollowUpReminderHours);
    }

    [Fact]
    public void NotificationPreferencesModel_ToHttpResponse_MapsAllFields() {
        var model = new NotificationPreferencesModel(true, false, true, 12, 20);

        var response = model.ToHttpResponse();

        Assert.True(response.PushNotificationsEnabled);
        Assert.False(response.FastingPushNotificationsEnabled);
        Assert.True(response.SocialPushNotificationsEnabled);
        Assert.Equal(12, response.FastingCheckInReminderHours);
        Assert.Equal(20, response.FastingCheckInFollowUpReminderHours);
    }

    [Fact]
    public void WebPushSubscription_ToHttpResponse_MapsAllFields() {
        var user = User.Create("mapping@example.com", "hash");
        var expiration = DateTime.UtcNow.AddDays(7);
        var subscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/123",
            "p256",
            "auth",
            expiration,
            "en",
            "Chrome");

        var response = subscription.ToHttpResponse();

        Assert.Equal(subscription.Endpoint, response.Endpoint);
        Assert.Equal("push.example.com", response.EndpointHost);
        Assert.Equal(expiration, response.ExpirationTimeUtc);
        Assert.Equal("en", response.Locale);
        Assert.Equal("Chrome", response.UserAgent);
        Assert.Equal(subscription.CreatedOnUtc, response.CreatedAtUtc);
        Assert.Equal(subscription.ModifiedOnUtc, response.UpdatedAtUtc);
    }

    [Fact]
    public void WebPushCommandsAndQueries_MapAllFields() {
        var userId = Guid.NewGuid();
        var expiration = DateTime.UtcNow.AddDays(7);
        var upsert = new UpsertWebPushSubscriptionHttpRequest(
            "https://push.example.com/subscriptions/123",
            expiration,
            new UpsertWebPushSubscriptionKeysHttpRequest("p256dh", "auth"),
            "ru",
            "Firefox").ToCommand(userId);
        var remove = new RemoveWebPushSubscriptionHttpRequest("https://push.example.com/subscriptions/123")
            .ToCommand(userId);
        var scheduled = new ScheduleTestNotificationHttpRequest(30, "fasting.completed").ToCommand(userId);

        Assert.NotNull(NotificationHttpMappings.ToWebPushConfigurationQuery());
        Assert.Equal(userId, userId.ToWebPushSubscriptionsQuery().UserId);
        Assert.Equal(userId, upsert.UserId);
        Assert.Equal("https://push.example.com/subscriptions/123", upsert.Endpoint);
        Assert.Equal("p256dh", upsert.P256Dh);
        Assert.Equal("auth", upsert.Auth);
        Assert.Equal(expiration, upsert.ExpirationTimeUtc);
        Assert.Equal("ru", upsert.Locale);
        Assert.Equal("Firefox", upsert.UserAgent);
        Assert.Equal(userId, remove.UserId);
        Assert.Equal("https://push.example.com/subscriptions/123", remove.Endpoint);
        Assert.Equal(userId, scheduled.UserId);
        Assert.Equal(30, scheduled.DelaySeconds);
        Assert.Equal("fasting.completed", scheduled.Type);
    }

    [Fact]
    public void WebPushSubscriptionModel_ToHttpResponse_MapsAllFields() {
        var createdAtUtc = DateTime.UtcNow.AddDays(-1);
        var updatedAtUtc = DateTime.UtcNow;
        var model = new WebPushSubscriptionModel(
            "not-a-uri",
            "not-a-uri",
            null,
            "en",
            "Chrome",
            createdAtUtc,
            updatedAtUtc);

        var response = model.ToHttpResponse();

        Assert.Equal("not-a-uri", response.Endpoint);
        Assert.Equal("not-a-uri", response.EndpointHost);
        Assert.Null(response.ExpirationTimeUtc);
        Assert.Equal("en", response.Locale);
        Assert.Equal("Chrome", response.UserAgent);
        Assert.Equal(createdAtUtc, response.CreatedAtUtc);
        Assert.Equal(updatedAtUtc, response.UpdatedAtUtc);
    }

    [Fact]
    public void ScheduledAndConfigurationModels_ToHttpResponse_MapAllFields() {
        var scheduledAtUtc = DateTime.UtcNow.AddMinutes(1);
        var scheduled = new ScheduledNotificationModel("fasting.completed", 45, scheduledAtUtc);
        var configuration = new WebPushConfigurationModel(true, "public-key");

        var scheduledResponse = scheduled.ToHttpResponse();
        var configurationResponse = configuration.ToHttpResponse();

        Assert.Equal("fasting.completed", scheduledResponse.Type);
        Assert.Equal(45, scheduledResponse.DelaySeconds);
        Assert.Equal(scheduledAtUtc, scheduledResponse.ScheduledAtUtc);
        Assert.True(configurationResponse.Enabled);
        Assert.Equal("public-key", configurationResponse.PublicKey);
    }
}
