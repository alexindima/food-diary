using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Notifications;

[ApiController]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<NotificationHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetNotifications([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToNotificationsQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetUnreadCount([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToUnreadCountQuery(), static value => new UnreadCountHttpResponse(value));

    [HttpPut("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkAsRead(Guid notificationId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(notificationId.ToMarkReadCommand(userId));

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> MarkAllAsRead([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToMarkAllReadCommand());

    [HttpPost("test/schedule")]
    [ProducesResponseType<ScheduledNotificationHttpResponse>(StatusCodes.Status202Accepted)]
    public Task<IActionResult> ScheduleTestNotification(
        [FromCurrentUser] Guid userId,
        [FromBody] ScheduleTestNotificationHttpRequest request) =>
        HandleAccepted(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpGet("preferences")]
    [ProducesResponseType<NotificationPreferencesHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetNotificationPreferences([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToNotificationPreferencesQuery(), static value => value.ToHttpResponse());

    [HttpPut("preferences")]
    [ProducesResponseType<NotificationPreferencesHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateNotificationPreferences(
        [FromCurrentUser] Guid userId,
        [FromBody] UpdateNotificationPreferencesHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());
}
