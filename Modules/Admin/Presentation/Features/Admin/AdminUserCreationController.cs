using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminUserCreationController(ISender mediator) : BaseApiController(mediator) {
    [HttpPost]
    [ProducesResponseType<AdminUserCreationHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> CreateUser(
        [FromCurrentUser] Guid actorUserId,
        [FromBody] AdminUserCreateHttpRequest request) =>
        HandleCreated(
            request.ToCommand(actorUserId, Request.Headers.Origin.ToString()),
            static value => value.ToHttpResponse());
}
