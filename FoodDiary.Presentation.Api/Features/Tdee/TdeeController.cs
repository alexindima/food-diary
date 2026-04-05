using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Tdee.Mappings;
using FoodDiary.Presentation.Api.Features.Tdee.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Tdee;

[ApiController]
[Route("api/v{version:apiVersion}/tdee")]
public class TdeeController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<TdeeInsightHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetInsight([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToTdeeQuery(), static value => value.ToHttpResponse());
}
