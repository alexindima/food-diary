using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Cycles;

[ApiController]
[Route("api/v{version:apiVersion}/cycles")]
public sealed class MenstrualEpisodesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPut("{cycleProfileId:guid}/period-start")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ConfirmPeriodStart(
        Guid cycleProfileId,
        [FromCurrentUser] Guid userId,
        [FromBody] ConfirmPeriodStartHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId), static value => value.ToHttpResponse());

    [HttpPut("{cycleProfileId:guid}/menstrual-episodes/{menstrualEpisodeId:guid}")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateMenstrualEpisode(
        [FromCurrentUser] Guid userId,
        Guid cycleProfileId,
        Guid menstrualEpisodeId,
        [FromBody] UpdateMenstrualEpisodeHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId, menstrualEpisodeId), static value => value.ToHttpResponse());

    [HttpDelete("{cycleProfileId:guid}/menstrual-episodes/{menstrualEpisodeId:guid}")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteMenstrualEpisode(
        [FromCurrentUser] Guid userId,
        Guid cycleProfileId,
        Guid menstrualEpisodeId) =>
        HandleOk(
            cycleProfileId.ToDeleteMenstrualEpisodeCommand(userId, menstrualEpisodeId),
            static value => value.ToHttpResponse());
}
