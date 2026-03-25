using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("info")]
    [ProducesResponseType<UserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUserInfo([FromCurrentUser] Guid userId) {
        var result = await Send(userId.ToUserQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("info")]
    [ProducesResponseType<UserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCurrentUser([FromCurrentUser] Guid userId, [FromBody] UpdateUserHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromCurrentUser] Guid userId, [FromBody] ChangePasswordHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Send(command);
        return result.ToNoContentActionResult();
    }

    [HttpGet("desired-weight")]
    [ProducesResponseType<UserDesiredWeightHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDesiredWeight([FromCurrentUser] Guid userId) {
        var result = await Send(userId.ToDesiredWeightQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("desired-weight")]
    [ProducesResponseType<UserDesiredWeightHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDesiredWeight([FromCurrentUser] Guid userId, [FromBody] UpdateDesiredWeightHttpRequest request) {
        var command = request.ToDesiredWeightCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet("desired-waist")]
    [ProducesResponseType<UserDesiredWaistHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDesiredWaist([FromCurrentUser] Guid userId) {
        var result = await Send(userId.ToDesiredWaistQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("desired-waist")]
    [ProducesResponseType<UserDesiredWaistHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDesiredWaist([FromCurrentUser] Guid userId, [FromBody] UpdateDesiredWaistHttpRequest request) {
        var command = request.ToDesiredWaistCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCurrentUser([FromCurrentUser] Guid userId) {
        var command = userId.ToDeleteCommand();
        var result = await Send(command);
        return result.ToNoContentActionResult();
    }
}

