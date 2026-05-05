using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminUsersController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<AdminUserHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetUsers([FromQuery] GetAdminUsersHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());

    [HttpGet("impersonation-sessions")]
    [ProducesResponseType<PagedHttpResponse<AdminImpersonationSessionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetImpersonationSessions([FromQuery] GetAdminImpersonationSessionsHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToImpersonationSessionsHttpResponse());

    [HttpGet("login-events")]
    [ProducesResponseType<PagedHttpResponse<AdminUserLoginEventHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetLoginEvents([FromQuery] GetAdminUserLoginEventsHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToLoginEventsHttpResponse());

    [HttpGet("login-summary")]
    [ProducesResponseType<IReadOnlyList<AdminUserLoginDeviceSummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetLoginSummary([FromQuery] GetAdminUserLoginSummaryHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<AdminUserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateHttpRequest request) =>
        HandleOk(request.ToCommand(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/impersonation")]
    [ProducesResponseType<AdminImpersonationStartHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> StartImpersonation(
        Guid id,
        [FromCurrentUser] Guid actorUserId,
        [FromBody] AdminImpersonationStartHttpRequest request) =>
        HandleOk(
            request.ToCommand(
                actorUserId,
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()),
            static value => value.ToHttpResponse());
}
