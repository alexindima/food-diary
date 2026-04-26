using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/v{version:apiVersion}/ai/usage")]
[Authorize]
public sealed class AiUsageController(ISender mediator, ILogger<AiUsageController> logger) : AuthorizedController(mediator) {
    private readonly ILogger<AiUsageController> _logger = logger;

    [HttpGet("me")]
    [ProducesResponseType<UserAiUsageHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyUsage([FromCurrentUser] Guid userId) =>
        HandleObservedOk(userId.ToUsageQuery(), static value => value.ToHttpResponse(), _logger, "ai.usage.me", userId);
}

