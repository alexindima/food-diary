using FoodDiary.Application.Notifications.Common;
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
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IWebPushConfigurationProvider webPushConfigurationProvider)
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
        return new ObjectResult(response) { StatusCode = StatusCodes.Status202Accepted };
    }

    [HttpGet("push/config")]
    [ProducesResponseType<WebPushConfigurationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetWebPushConfiguration() {
        var configuration = webPushConfigurationProvider.GetClientConfiguration();
        IActionResult response = new OkObjectResult(new WebPushConfigurationHttpResponse(configuration.Enabled, configuration.PublicKey));
        return Task.FromResult(response);
    }

    [HttpPut("push/subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertWebPushSubscription(
        [FromCurrentUser] Guid userId,
        [FromBody] UpsertWebPushSubscriptionHttpRequest request) {
        if (string.IsNullOrWhiteSpace(request.Endpoint)
            || string.IsNullOrWhiteSpace(request.Keys?.P256dh)
            || string.IsNullOrWhiteSpace(request.Keys.Auth)) {
            return new BadRequestObjectResult(new ApiErrorHttpResponse(
                "Validation.Invalid",
                "Endpoint and subscription keys are required.",
                HttpContext.TraceIdentifier));
        }

        var existing = await webPushSubscriptionRepository.GetByEndpointAsync(request.Endpoint, asTracking: true, HttpContext.RequestAborted);
        if (existing is null) {
            var subscription = FoodDiary.Domain.Entities.Notifications.WebPushSubscription.Create(
                new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
                request.Endpoint,
                request.Keys.P256dh,
                request.Keys.Auth,
                request.ExpirationTime,
                request.Locale,
                request.UserAgent);

            await webPushSubscriptionRepository.AddAsync(subscription, HttpContext.RequestAborted);
            return new NoContentResult();
        }

        existing.Refresh(
            new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
            request.Keys.P256dh,
            request.Keys.Auth,
            request.ExpirationTime,
            request.Locale,
            request.UserAgent);

        await webPushSubscriptionRepository.UpdateAsync(existing, HttpContext.RequestAborted);
        return new NoContentResult();
    }

    [HttpDelete("push/subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveWebPushSubscription(
        [FromCurrentUser] Guid userId,
        [FromBody] RemoveWebPushSubscriptionHttpRequest request) {
        if (string.IsNullOrWhiteSpace(request.Endpoint)) {
            return new NoContentResult();
        }

        var existing = await webPushSubscriptionRepository.GetByEndpointAsync(request.Endpoint, asTracking: true, HttpContext.RequestAborted);
        if (existing is null || existing.UserId.Value != userId) {
            return new NoContentResult();
        }

        await webPushSubscriptionRepository.DeleteAsync(existing, HttpContext.RequestAborted);
        return new NoContentResult();
    }
}
