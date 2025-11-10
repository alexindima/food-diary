using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Contracts.Users;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("info")]
    public async Task<IActionResult> GetCurrentUserInfo() {
        var query = new GetUserByIdQuery(CurrentUserId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch("info")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request) {
        var command = request.ToCommand(CurrentUserId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) {
        var command = request.ToCommand(CurrentUserId);
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }
}
