using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Gamification.Mappings;
using FoodDiary.Presentation.Api.Features.Gamification.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Gamification;

[ApiController]
[Route("api/v{version:apiVersion}/gamification")]
public class GamificationController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<GamificationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.ToHttpResponse());
}
