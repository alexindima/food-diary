using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Cycles;

[ApiController]
[Route("api/v{version:apiVersion}/cycles")]
public class CyclesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("current")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetCurrent([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToCurrentQuery(), static value => value is null ? null : value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateCycleHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{cycleProfileId:guid}/days")]
    [ProducesResponseType<CycleLogDayHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpsertDay(Guid cycleProfileId, [FromCurrentUser] Guid userId, [FromBody] UpsertCycleDayHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId), static value => value.ToHttpResponse());

    [HttpPut("{cycleProfileId:guid}/factors")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpsertFactor(Guid cycleProfileId, [FromCurrentUser] Guid userId, [FromBody] UpsertCycleFactorHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId), static value => value.ToHttpResponse());
}
