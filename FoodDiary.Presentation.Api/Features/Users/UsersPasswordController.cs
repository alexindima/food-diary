using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/v{version:apiVersion}/users/password")]
public class UsersPasswordController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ChangePassword([FromCurrentUser] Guid userId, [FromBody] ChangePasswordHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpPatch("set")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetPassword([FromCurrentUser] Guid userId, [FromBody] SetPasswordHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));
}
