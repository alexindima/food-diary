using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Authorize(Roles = PresentationRoleNames.Dietologist)]
[Route("api/v{version:apiVersion}/dietologist/clients")]
public sealed class DietologistClientTasksController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("{clientUserId:guid}/tasks")]
    [ProducesResponseType<List<ClientTaskHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetTasksForClient(Guid clientUserId, [FromCurrentUser] Guid userId) =>
        HandleOk(clientUserId.ToClientTasksQuery(userId), static value => value.Select(task => task.ToHttpResponse()).ToList());

    [HttpPost("{clientUserId:guid}/tasks")]
    [ProducesResponseType<ClientTaskHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> CreateTask(
        Guid clientUserId,
        [FromCurrentUser] Guid userId,
        [FromBody] CreateClientTaskHttpRequest request) =>
        HandleCreated(
            request.ToCommand(userId, clientUserId),
            static value => value.ToHttpResponse());

    [HttpPut("tasks/{taskId:guid}/cancel")]
    [ProducesResponseType<ClientTaskHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> CancelTask(Guid taskId, [FromCurrentUser] Guid userId) =>
        HandleOk(taskId.ToCancelClientTaskCommand(userId), static value => value.ToHttpResponse());
}
