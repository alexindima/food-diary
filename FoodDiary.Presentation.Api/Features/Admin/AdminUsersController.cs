using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminUsersController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] GetAdminUsersHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateHttpRequest request) {
        var command = request.ToCommand(id);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
