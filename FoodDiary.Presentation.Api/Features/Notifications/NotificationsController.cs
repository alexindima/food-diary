using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Notifications;

[ApiController]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController(
    ISender mediator,
    INotificationTestScheduler notificationTestScheduler,
    IAuditLogger auditLogger)
    : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<NotificationHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetNotifications([FromCurrentUser] Guid userId) {
        return HandleOk(userId.ToNotificationsQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());
    }

    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetUnreadCount([FromCurrentUser] Guid userId) {
        return HandleOk(userId.ToUnreadCountQuery(), static value => new UnreadCountHttpResponse(value));
    }

    [HttpPut("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkAsRead(Guid notificationId, [FromCurrentUser] Guid userId) {
        return HandleNoContent(notificationId.ToMarkReadCommand(userId));
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> MarkAllAsRead([FromCurrentUser] Guid userId) {
        return HandleNoContent(userId.ToMarkAllReadCommand());
    }

    [HttpPost("test/schedule")]
    [ProducesResponseType<ScheduledNotificationHttpResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ScheduleTestNotification(
        [FromCurrentUser] Guid userId,
        [FromBody] ScheduleTestNotificationHttpRequest request) {
        var response = await notificationTestScheduler.ScheduleAsync(userId, request.DelaySeconds, request.Type);
        auditLogger.Log(
            "notifications.test.scheduled",
            new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
            "Notification",
            request.Type,
            $"delaySeconds={response.DelaySeconds};scheduledAtUtc={response.ScheduledAtUtc:O}");
        return new ObjectResult(response) { StatusCode = StatusCodes.Status202Accepted };
    }

    [HttpGet("preferences")]
    [ProducesResponseType<NotificationPreferencesHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetNotificationPreferences([FromCurrentUser] Guid userId) {
        return HandleOk(userId.ToNotificationPreferencesQuery(), static value => value.ToHttpResponse());
    }

    [HttpPut("preferences")]
    [ProducesResponseType<NotificationPreferencesHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateNotificationPreferences(
        [FromCurrentUser] Guid userId,
        [FromBody] UpdateNotificationPreferencesHttpRequest request) {
        return HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());
    }
}
