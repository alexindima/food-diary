using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using FoodDiary.Presentation.Api.Features.Dashboard.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Authorize(Roles = PresentationRoleNames.Dietologist)]
[Route("api/v{version:apiVersion}/dietologist/clients")]
public class DietologistClientsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<ClientSummaryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyClients([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToMyClientsQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpDelete("{clientUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DisconnectClient(Guid clientUserId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(new DisconnectClientHttpRequest(clientUserId).ToCommand(userId));

    [HttpGet("{clientUserId:guid}/dashboard")]
    [ProducesResponseType<DashboardSnapshotHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetClientDashboard(
        Guid clientUserId,
        [FromCurrentUser] Guid userId,
        [FromQuery] GetClientDashboardHttpQuery query) =>
        HandleOk(query.ToClientDashboardQuery(userId, clientUserId), static value => value.ToHttpResponse());

    [HttpGet("{clientUserId:guid}/goals")]
    [ProducesResponseType<UserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetClientGoals(Guid clientUserId, [FromCurrentUser] Guid userId) =>
        HandleOk(clientUserId.ToClientGoalsQuery(userId), static value => value.ToHttpResponse());
}
