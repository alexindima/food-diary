using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("info")]
    public async Task<IActionResult> GetCurrentUserInfo([FromCurrentUser] UserId userId) {
        var query = new GetUserByIdQuery(userId);
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPatch("info")]
    public async Task<IActionResult> UpdateCurrentUser([FromCurrentUser] UserId userId, [FromBody] UpdateUserHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromCurrentUser] UserId userId, [FromBody] ChangePasswordHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpGet("desired-weight")]
    public async Task<IActionResult> GetDesiredWeight([FromCurrentUser] UserId userId) {
        var query = new GetDesiredWeightQuery(userId);
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPut("desired-weight")]
    public async Task<IActionResult> UpdateDesiredWeight([FromCurrentUser] UserId userId, [FromBody] UpdateDesiredWeightHttpRequest request) {
        var command = new UpdateDesiredWeightCommand(userId, request.DesiredWeight);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpGet("desired-waist")]
    public async Task<IActionResult> GetDesiredWaist([FromCurrentUser] UserId userId) {
        var query = new GetDesiredWaistQuery(userId);
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPut("desired-waist")]
    public async Task<IActionResult> UpdateDesiredWaist([FromCurrentUser] UserId userId, [FromBody] UpdateDesiredWaistHttpRequest request) {
        var command = new UpdateDesiredWaistCommand(userId, request.DesiredWaist);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCurrentUser([FromCurrentUser] UserId userId) {
        var command = new DeleteUserCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
