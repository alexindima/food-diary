using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
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
}
