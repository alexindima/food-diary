using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;

namespace FoodDiary.Presentation.Api.Tests;

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
        var model = new NotificationModel(id, "NewRecommendation", "New recommendation", "Details here", "ref-123", false, createdAt);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("NewRecommendation", response.Type);
        Assert.Equal("New recommendation", response.Title);
        Assert.Equal("Details here", response.Body);
        Assert.Equal("ref-123", response.ReferenceId);
        Assert.False(response.IsRead);
        Assert.Equal(createdAt, response.CreatedAtUtc);
    }

    [Fact]
    public void NotificationModel_ToHttpResponse_WithNullOptionalFields() {
        var model = new NotificationModel(Guid.NewGuid(), "info", "Title", null, null, true, DateTime.UtcNow);

        var response = model.ToHttpResponse();

        Assert.Null(response.Body);
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
        var request = new UpdateNotificationPreferencesHttpRequest(true, false, true);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.PushNotificationsEnabled, command.PushNotificationsEnabled);
        Assert.Equal(request.FastingPushNotificationsEnabled, command.FastingPushNotificationsEnabled);
        Assert.Equal(request.SocialPushNotificationsEnabled, command.SocialPushNotificationsEnabled);
    }

    [Fact]
    public void NotificationPreferencesModel_ToHttpResponse_MapsAllFields() {
        var model = new NotificationPreferencesModel(true, false, true);

        var response = model.ToHttpResponse();

        Assert.True(response.PushNotificationsEnabled);
        Assert.False(response.FastingPushNotificationsEnabled);
        Assert.True(response.SocialPushNotificationsEnabled);
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
}
