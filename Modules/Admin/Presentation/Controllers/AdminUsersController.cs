using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPremiumGrant;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUserPremiumGrant;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminUsersController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<AdminUserHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetUsers([FromQuery] GetAdminUsersHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminUserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetUser(Guid id) =>
        HandleOk(id.ToAdminUserQuery(), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}/premium-grant")]
    [ProducesResponseType<AdminUserPremiumGrantHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetUserPremiumGrant(Guid id) =>
        HandleOk(new GetAdminUserPremiumGrantQuery(id), static grant => new AdminUserPremiumGrantHttpResponse(grant.EndsAtUtc, grant.Reason));

    [HttpPut("{id:guid}/premium-grant")]
    [ProducesResponseType<AdminUserPremiumGrantHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetUserPremiumGrant(Guid id, [FromCurrentUser] Guid actorUserId, [FromBody] AdminUserPremiumGrantHttpRequest request) =>
        HandleOk(new SetAdminUserPremiumGrantCommand(id, actorUserId, request.EndsAtUtc, request.Reason),
            static grant => new AdminUserPremiumGrantHttpResponse(grant.EndsAtUtc, grant.Reason));

    [HttpDelete("{id:guid}/premium-grant")]
    [ProducesResponseType<AdminUserPremiumGrantHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RevokeUserPremiumGrant(Guid id, [FromCurrentUser] Guid actorUserId) =>
        HandleOk(new SetAdminUserPremiumGrantCommand(id, actorUserId, EndsAtUtc: null, Reason: null),
            static grant => new AdminUserPremiumGrantHttpResponse(grant.EndsAtUtc, grant.Reason));

    [HttpGet("{id:guid}/role-audit")]
    [ProducesResponseType<IReadOnlyList<AdminUserRoleAuditEventHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetUserRoleAudit(Guid id, [FromQuery] GetAdminUserRoleAuditHttpQuery query) =>
        HandleOk(query.ToRoleAuditQuery(id), static value => value.Select(item => item.ToHttpResponse()).ToList());

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
    public Task<IActionResult> UpdateUser(
        Guid id,
        [FromCurrentUser] Guid actorUserId,
        [FromBody] AdminUserUpdateHttpRequest request) =>
        HandleOk(request.ToCommand(id, actorUserId), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/impersonation")]
    [ProducesResponseType<AdminImpersonationStartHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> StartImpersonation(
        Guid id,
        [FromCurrentUser] Guid actorUserId,
        [FromCurrentRefreshSession] Guid actorSessionId,
        [FromBody] AdminImpersonationStartHttpRequest request) =>
        HandleOk(
            request.ToCommand(
                actorUserId,
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                actorSessionId),
            static value => value.ToHttpResponse());
}
