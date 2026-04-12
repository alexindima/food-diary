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
    public Task<IActionResult> SkipCyclicDay([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToSkipCyclicDayCommand(), static value => value.ToHttpResponse());

    [HttpPut("current/postpone-day")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> PostponeCyclicDay([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToPostponeCyclicDayCommand(), static value => value.ToHttpResponse());

}
