using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("info")]
    [ProducesResponseType<UserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetCurrentUserInfo([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToUserQuery(), static value => value.ToHttpResponse());

    [HttpPatch("info")]
    [ProducesResponseType<UserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateCurrentUser([FromCurrentUser] Guid userId, [FromBody] UpdateUserHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPatch("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ChangePassword([FromCurrentUser] Guid userId, [FromBody] ChangePasswordHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpPatch("password/set")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetPassword([FromCurrentUser] Guid userId, [FromBody] SetPasswordHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpGet("desired-weight")]
    [ProducesResponseType<UserDesiredWeightHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetDesiredWeight([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToDesiredWeightQuery(), static value => value.ToHttpResponse());

    [HttpPut("desired-weight")]
    [ProducesResponseType<UserDesiredWeightHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateDesiredWeight([FromCurrentUser] Guid userId, [FromBody] UpdateDesiredWeightHttpRequest request) =>
        HandleOk(request.ToDesiredWeightCommand(userId), static value => value.ToHttpResponse());

    [HttpGet("desired-waist")]
    [ProducesResponseType<UserDesiredWaistHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetDesiredWaist([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToDesiredWaistQuery(), static value => value.ToHttpResponse());

    [HttpPut("desired-waist")]
    [ProducesResponseType<UserDesiredWaistHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateDesiredWaist([FromCurrentUser] Guid userId, [FromBody] UpdateDesiredWaistHttpRequest request) =>
        HandleOk(request.ToDesiredWaistCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteCurrentUser([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToDeleteCommand());
}
