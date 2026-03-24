using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Contracts.Users;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("info")]
    public async Task<IActionResult> GetCurrentUserInfo() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetUserByIdQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch("info")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpGet("desired-weight")]
    public async Task<IActionResult> GetDesiredWeight() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetDesiredWeightQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPut("desired-weight")]
    public async Task<IActionResult> UpdateDesiredWeight([FromBody] UpdateDesiredWeightRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new UpdateDesiredWeightCommand(userId, request.DesiredWeight);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpGet("desired-waist")]
    public async Task<IActionResult> GetDesiredWaist() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetDesiredWaistQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPut("desired-waist")]
    public async Task<IActionResult> UpdateDesiredWaist([FromBody] UpdateDesiredWaistRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new UpdateDesiredWaistCommand(userId, request.DesiredWaist);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCurrentUser() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new DeleteUserCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
