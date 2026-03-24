using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Domain.Enums;
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
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminUsersController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false) {
        var query = new GetAdminUsersQuery(page, limit, search, includeDeleted);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateHttpRequest request) {
        var command = request.ToCommand(id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
