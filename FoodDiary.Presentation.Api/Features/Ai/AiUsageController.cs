using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
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
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyUsage([FromCurrentUser] Guid userId) {
        var result = await Send(userId.ToUsageQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}

