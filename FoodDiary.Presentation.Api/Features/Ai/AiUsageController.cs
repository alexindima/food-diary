using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/ai/usage")]
[Authorize]
public sealed class AiUsageController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("me")]
    [ProducesResponseType<UserAiUsageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyUsage([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToUsageQuery(), static value => value.ToHttpResponse());
}

