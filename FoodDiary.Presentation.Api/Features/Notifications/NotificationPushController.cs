using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Notifications;

[ApiController]
[Route("api/v{version:apiVersion}/notifications/push")]
public class NotificationPushController(
    ISender mediator,
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IWebPushConfigurationProvider webPushConfigurationProvider,
    IAuditLogger auditLogger,
    ILogger<NotificationPushController> logger)
    : AuthorizedController(mediator) {
    [HttpGet("config")]
    [ProducesResponseType<WebPushConfigurationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetWebPushConfiguration() {
        var configuration = webPushConfigurationProvider.GetClientConfiguration();
        IActionResult response = new OkObjectResult(new WebPushConfigurationHttpResponse(configuration.Enabled, configuration.PublicKey));
        return Task.FromResult(response);
    }

    [HttpGet("subscriptions")]
    [ProducesResponseType<List<WebPushSubscriptionHttpResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebPushSubscriptions([FromCurrentUser] Guid userId) {
        var subscriptions = await webPushSubscriptionRepository.GetByUserAsync(
            new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
            HttpContext.RequestAborted);

        var utcNow = DateTime.UtcNow;
        var expiredSubscriptions = subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc.HasValue && subscription.ExpirationTimeUtc.Value <= utcNow)
            .ToList();

        if (expiredSubscriptions.Count > 0) {
            await webPushSubscriptionRepository.DeleteRangeAsync(expiredSubscriptions, HttpContext.RequestAborted);
            logger.LogInformation(
                "Pruned {SubscriptionCount} expired web push subscriptions while listing subscriptions for user {UserId}.",
                expiredSubscriptions.Count,
                userId);
            subscriptions = subscriptions.Except(expiredSubscriptions).ToList();
        }

        return new OkObjectResult(subscriptions.Select(x => x.ToHttpResponse()).ToList());
    }

    [HttpPut("subscription")]
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
            auditLogger.Log(
                "notifications.push-subscription.connected",
                new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
                "WebPushSubscription",
                subscription.Id.Value.ToString(),
                $"endpointHost={GetEndpointHost(request.Endpoint)};locale={request.Locale ?? "-"}");
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
        auditLogger.Log(
            "notifications.push-subscription.refreshed",
            new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
            "WebPushSubscription",
            existing.Id.Value.ToString(),
            $"endpointHost={GetEndpointHost(existing.Endpoint)};locale={request.Locale ?? "-"}");
        return new NoContentResult();
    }

    [HttpDelete("subscription")]
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
        auditLogger.Log(
            "notifications.push-subscription.disconnected",
            new FoodDiary.Domain.ValueObjects.Ids.UserId(userId),
            "WebPushSubscription",
            existing.Id.Value.ToString(),
            $"endpointHost={GetEndpointHost(existing.Endpoint)}");
        return new NoContentResult();
    }

    private static string GetEndpointHost(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            ? uri.Host
            : endpoint;
    }
}
