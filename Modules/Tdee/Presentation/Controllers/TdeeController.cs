using FoodDiary.Modules.Tdee.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Tdee.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Tdee.Presentation.Contracts.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Tdee.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/tdee")]
public sealed class TdeeController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<TdeeInsightHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetInsight([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToTdeeQuery(), static value => value.ToHttpResponse());
}
