using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Usda.Mappings;
using FoodDiary.Presentation.Api.Features.Usda.Requests;
using FoodDiary.Presentation.Api.Features.Usda.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Usda;

[ApiController]
[Route("api/v{version:apiVersion}/usda")]
public class UsdaController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("foods")]
    [ProducesResponseType<IReadOnlyList<UsdaFoodHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Search(
        [FromQuery] string search,
        [FromQuery] int limit = 20) =>
        HandleOk(UsdaHttpMappings.ToQuery(search, limit), static value => value.ToHttpResponse());

    [HttpGet("foods/{fdcId:int}")]
    [ProducesResponseType<UsdaFoodDetailHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetDetail(int fdcId) =>
        HandleOk(UsdaHttpMappings.ToQuery(fdcId), static value => value.ToHttpResponse());

    [HttpPut("products/{productId:guid}/link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> LinkProduct(
        [FromCurrentUser] Guid userId,
        Guid productId,
        [FromBody] LinkProductToUsdaFoodHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId, productId));

    [HttpDelete("products/{productId:guid}/link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> UnlinkProduct(
        [FromCurrentUser] Guid userId,
        Guid productId) =>
        HandleNoContent(UsdaHttpMappings.ToUnlinkCommand(userId, productId));

    [HttpGet("daily-micronutrients")]
    [ProducesResponseType<DailyMicronutrientSummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetDailyMicronutrients(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime date) =>
        HandleOk(UsdaHttpMappings.ToDailyQuery(userId, date), static value => value.ToHttpResponse());
}
