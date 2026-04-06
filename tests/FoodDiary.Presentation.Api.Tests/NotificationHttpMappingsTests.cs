using FoodDiary.Application.Notifications.Models;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;

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
}
