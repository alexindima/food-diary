using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users/{id:guid}/premium-grant")]
[Authorize(Roles = PresentationRoleNames.Admin)]
[Tags("AdminUsers")]
public sealed class AdminUserPremiumGrantController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<AdminUserPremiumGrantHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetUserPremiumGrant(Guid id) =>
        HandleOk(id.ToPremiumGrantQuery(), static grant => grant.ToHttpResponse());

    [HttpPut]
    [ProducesResponseType<AdminUserPremiumGrantHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetUserPremiumGrant(Guid id, [FromCurrentUser] Guid actorUserId, [FromBody] AdminUserPremiumGrantHttpRequest request) =>
        HandleOk(request.ToSetPremiumGrantCommand(id, actorUserId), static grant => grant.ToHttpResponse());

    [HttpDelete]
    [ProducesResponseType<AdminUserPremiumGrantHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RevokeUserPremiumGrant(Guid id, [FromCurrentUser] Guid actorUserId) =>
        HandleOk(id.ToRevokePremiumGrantCommand(actorUserId), static grant => grant.ToHttpResponse());
}
