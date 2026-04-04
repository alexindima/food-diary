using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Notifications;

[ApiController]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController(ISender mediator) : AuthorizedController(mediator) {
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
}
