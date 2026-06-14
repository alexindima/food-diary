using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;
using FoodDiary.Application.Notifications.Queries.GetNotifications;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Notifications;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationsControllerTests {
    [Fact]
    public async Task GetNotifications_SendsQueryAndReturnsNotifications() {
        var notification = new NotificationModel(
            Guid.NewGuid(),
            "info",
            "Title",
            "Body",
            "/target",
            "ref-1",
            IsRead: false,
            DateTime.UtcNow);
        IRequest<Result<IReadOnlyList<NotificationModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<NotificationModel>>([notification]), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetNotifications(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<NotificationHttpResponse> response = Assert.IsType<List<NotificationHttpResponse>>(ok.Value);
        Assert.Single(response);
        Assert.Equal(notification.Id, response[0].Id);
        GetNotificationsQuery query = Assert.IsType<GetNotificationsQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task GetUnreadCount_SendsQueryAndReturnsCountResponse() {
        IRequest<Result<int>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(7), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetUnreadCount(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UnreadCountHttpResponse response = Assert.IsType<UnreadCountHttpResponse>(ok.Value);
        Assert.Equal(7, response.Count);
        GetUnreadCountQuery query = Assert.IsType<GetUnreadCountQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task ScheduleTestNotification_SendsCommandAndReturnsAcceptedResponse() {
        DateTime scheduledAtUtc = DateTime.UtcNow.AddSeconds(45);
        var model = new ScheduledNotificationModel("fasting.completed", 45, scheduledAtUtc);
        IRequest<Result<ScheduledNotificationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new ScheduleTestNotificationHttpRequest(45, "fasting.completed");

        IActionResult result = await controller.ScheduleTestNotification(userId, request);

        AcceptedResult accepted = Assert.IsType<AcceptedResult>(result);
        ScheduledNotificationHttpResponse response = Assert.IsType<ScheduledNotificationHttpResponse>(accepted.Value);
        Assert.Equal("fasting.completed", response.Type);
        Assert.Equal(45, response.DelaySeconds);
        Assert.Equal(scheduledAtUtc, response.ScheduledAtUtc);
        ScheduleTestNotificationCommand command = Assert.IsType<ScheduleTestNotificationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(45, command.DelaySeconds);
        Assert.Equal("fasting.completed", command.Type);
    }

    [Fact]
    public async Task MarkAsRead_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        IActionResult result = await controller.MarkAsRead(notificationId, userId);

        Assert.IsType<NoContentResult>(result);
        MarkNotificationReadCommand command = Assert.IsType<MarkNotificationReadCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(notificationId, command.NotificationId);
    }

    [Fact]
    public async Task MarkAllAsRead_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.MarkAllAsRead(userId);

        Assert.IsType<NoContentResult>(result);
        MarkAllNotificationsReadCommand command = Assert.IsType<MarkAllNotificationsReadCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task GetNotificationPreferences_SendsQueryAndReturnsPreferences() {
        var model = new NotificationPreferencesModel(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 20);
        IRequest<Result<NotificationPreferencesModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetNotificationPreferences(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        NotificationPreferencesHttpResponse response = Assert.IsType<NotificationPreferencesHttpResponse>(ok.Value);
        Assert.True(response.PushNotificationsEnabled);
        GetNotificationPreferencesQuery query = Assert.IsType<GetNotificationPreferencesQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_SendsCommandAndReturnsPreferences() {
        var model = new NotificationPreferencesModel(
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: false,
            FastingCheckInReminderHours: 10,
            FastingCheckInFollowUpReminderHours: 18);
        IRequest<Result<NotificationPreferencesModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        NotificationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new UpdateNotificationPreferencesHttpRequest(
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: false,
            FastingCheckInReminderHours: 10,
            FastingCheckInFollowUpReminderHours: 18);

        IActionResult result = await controller.UpdateNotificationPreferences(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        NotificationPreferencesHttpResponse response = Assert.IsType<NotificationPreferencesHttpResponse>(ok.Value);
        Assert.True(response.FastingPushNotificationsEnabled);
        UpdateNotificationPreferencesCommand command = Assert.IsType<UpdateNotificationPreferencesCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(10, command.FastingCheckInReminderHours);
    }

    private static NotificationsController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
