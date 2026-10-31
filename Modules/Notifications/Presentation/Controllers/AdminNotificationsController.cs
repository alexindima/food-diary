using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;
using FoodDiary.Modules.Notifications.Presentation.Mappings;
using FoodDiary.Modules.Notifications.Presentation.Requests;
using FoodDiary.Modules.Notifications.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users/{id:guid}/notifications")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminNotificationsController(ISender mediator) : BaseApiController(mediator) {
    [HttpPost("test")]
    [ProducesResponseType<ScheduledNotificationHttpResponse>(StatusCodes.Status202Accepted)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableIdempotency]
    [EnableRateLimiting(PresentationPolicyNames.TestDeliveryRateLimitPolicyName)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> ScheduleTestNotification(
        Guid id,
        [FromCurrentUser] Guid actorUserId,
        [FromBody] ScheduleTestNotificationHttpRequest request) =>
        HandleAccepted(
            new ScheduleAdminTestNotificationCommand(actorUserId, id, request.DelaySeconds, request.Type),
            static value => value.ToHttpResponse());
}
