using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Ai.Presentation.Mappings;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Ai.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/ai/usage")]
[Authorize]
public sealed class AiUsageController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("me")]
    [ProducesResponseType<UserAiUsageHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyUsage([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToUsageQuery(), static value => value.ToHttpResponse());
}
