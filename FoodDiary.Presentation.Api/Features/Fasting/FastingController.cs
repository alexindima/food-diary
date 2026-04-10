using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Fasting.Mappings;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Fasting;

[ApiController]
[Route("api/v{version:apiVersion}/fasting")]
public class FastingController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("start")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Start([FromCurrentUser] Guid userId, [FromBody] StartFastingHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("end")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> End([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToEndCommand(), static value => value.ToHttpResponse());

    [HttpPut("current/duration")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ExtendDuration([FromCurrentUser] Guid userId, [FromBody] ExtendActiveFastingHttpRequest request) =>
        HandleOk(request.ToExtendCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("current/check-in")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateCheckIn([FromCurrentUser] Guid userId, [FromBody] UpdateFastingCheckInHttpRequest request) =>
        HandleOk(request.ToCheckInCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("current/skip-day")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SkipCyclicFastDay([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToSkipCyclicFastDayCommand(), static value => value.ToHttpResponse());

    [HttpPut("current/postpone-day")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> PostponeCyclicFastDay([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToPostponeCyclicFastDayCommand(), static value => value.ToHttpResponse());

    [HttpGet("current")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetCurrent([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToCurrentQuery(), static value => value?.ToHttpResponse());

    [HttpGet("history")]
    [ProducesResponseType<List<FastingSessionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId, [FromQuery] GetFastingHistoryHttpQuery query) =>
        HandleOk(query.ToHistoryQuery(userId), static value => value.Select(x => x.ToHttpResponse()).ToList());

}
