using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Route("api/v{version:apiVersion}/client-tasks")]
public sealed class ClientTasksController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<ClientTaskHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyTasks([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToMyClientTasksQuery(), static value => value.Select(task => task.ToHttpResponse()).ToList());

    [HttpPut("{taskId:guid}/status")]
    [ProducesResponseType<ClientTaskHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ChangeStatus(
        Guid taskId,
        [FromCurrentUser] Guid userId,
        [FromBody] ChangeClientTaskStatusHttpRequest request) =>
        HandleOk(request.ToCommand(userId, taskId), static value => value.ToHttpResponse());
}
