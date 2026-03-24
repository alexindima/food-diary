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
        var result = await Mediator.Send(userId.ToUserQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("info")]
    public async Task<IActionResult> UpdateCurrentUser([FromCurrentUser] UserId userId, [FromBody] UpdateUserHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromCurrentUser] UserId userId, [FromBody] ChangePasswordHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }

    [HttpGet("desired-weight")]
    public async Task<IActionResult> GetDesiredWeight([FromCurrentUser] UserId userId) {
        var result = await Mediator.Send(userId.ToDesiredWeightQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("desired-weight")]
    public async Task<IActionResult> UpdateDesiredWeight([FromCurrentUser] UserId userId, [FromBody] UpdateDesiredWeightHttpRequest request) {
        var command = request.ToDesiredWeightCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet("desired-waist")]
    public async Task<IActionResult> GetDesiredWaist([FromCurrentUser] UserId userId) {
        var result = await Mediator.Send(userId.ToDesiredWaistQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("desired-waist")]
    public async Task<IActionResult> UpdateDesiredWaist([FromCurrentUser] UserId userId, [FromBody] UpdateDesiredWaistHttpRequest request) {
        var command = request.ToDesiredWaistCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCurrentUser([FromCurrentUser] UserId userId) {
        var command = userId.ToDeleteCommand();
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }
}
