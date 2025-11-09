using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetById(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid userId format");

        var query = new GetUserByIdQuery(new UserId(userGuid));
        var result = await mediator.Send(query);
        return result.ToActionResult();
    }

    /// <summary>
    /// Обновить профиль пользователя
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(string userId, [FromBody] UpdateUserRequest request)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid userId format");

        var command = request.ToCommand(new UserId(userGuid));
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }

    // TODO: Добавить [Authorize] и извлекать userId из JWT claims
}
