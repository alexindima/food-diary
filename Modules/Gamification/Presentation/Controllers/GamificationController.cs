using System.ComponentModel.DataAnnotations;
using FoodDiary.Modules.Gamification.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Gamification.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Gamification.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/gamification")]
public sealed class GamificationController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<GamificationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery, MaxLength(100)] string? timeZoneId = null) =>
        HandleOk(userId.ToQuery(timeZoneId), static value => value.ToHttpResponse());
}
