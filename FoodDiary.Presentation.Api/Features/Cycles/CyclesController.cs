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
public sealed class CyclesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("current")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetCurrent([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToCurrentQuery(), static value => value?.ToHttpResponse());

    [HttpGet("current/nutrition-summary")]
    [ProducesResponseType<CycleNutritionSummaryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetNutritionSummary(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo) =>
        HandleOk(userId.ToNutritionSummaryQuery(dateFrom, dateTo), static value => value is null ? null : value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateCycleHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete("{cycleProfileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid cycleProfileId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(cycleProfileId.ToDeleteCommand(userId));

    [HttpPut("{cycleProfileId:guid}/settings")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateSettings(
        Guid cycleProfileId,
        [FromCurrentUser] Guid userId,
        [FromBody] UpdateCycleSettingsHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId), static value => value.ToHttpResponse());

    [HttpPut("{cycleProfileId:guid}/consents/{purpose:int}")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateConsent(
        Guid cycleProfileId,
        int purpose,
        [FromCurrentUser] Guid userId,
        [FromBody] UpdateCycleConsentHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId, purpose), static value => value.ToHttpResponse());

}
