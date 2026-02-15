using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Contracts.Admin;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminUsersController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetAdminUsersQuery(page, limit, search, includeDeleted);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateRequest request)
    {
        var command = new UpdateAdminUserCommand(
            new UserId(id),
            request.IsActive,
            request.IsEmailConfirmed,
            request.Roles ?? Array.Empty<string>(),
            request.Language,
            request.AiInputTokenLimit,
            request.AiOutputTokenLimit);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
