using FoodDiary.Presentation.Api.Security;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Notifications.Presentation.Mappings;
using FoodDiary.Modules.Notifications.Presentation.Requests;
using FoodDiary.Modules.Notifications.Presentation.Contracts.Responses;
using FoodDiary.Modules.Notifications.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/notifications/push")]
public sealed class NotificationPushController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("config")]
    [ProducesResponseType<WebPushConfigurationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetWebPushConfiguration() =>
        HandleOk(NotificationHttpMappings.ToWebPushConfigurationQuery(), static value => value.ToHttpResponse());

    [HttpGet("subscriptions")]
    [ProducesResponseType<List<WebPushSubscriptionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetWebPushSubscriptions([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToWebPushSubscriptionsQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpPut("subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> UpsertWebPushSubscription(
        [FromCurrentUser] Guid userId,
        [FromBody] UpsertWebPushSubscriptionHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpDelete("subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> RemoveWebPushSubscription(
        [FromCurrentUser] Guid userId,
        [FromBody] RemoveWebPushSubscriptionHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));
}
