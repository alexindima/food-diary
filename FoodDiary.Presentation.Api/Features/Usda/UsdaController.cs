using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Usda.Mappings;
using FoodDiary.Presentation.Api.Features.Usda.Requests;
using FoodDiary.Presentation.Api.Features.Usda.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Usda;

[ApiController]
[Route("api/v{version:apiVersion}/usda")]
public sealed class UsdaController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("foods")]
    [EnableRateLimiting(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
    [ProducesResponseType<IReadOnlyList<UsdaFoodHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Search(
        [FromQuery, MaxLength(UsdaRequestLimits.MaximumSearchLength)] string search,
        [FromQuery, Range(UsdaRequestLimits.MinimumLimit, UsdaRequestLimits.MaximumLimit)] int limit = 20) =>
        HandleOk(UsdaHttpMappings.ToQuery(search, limit), static value => value.ToHttpResponse());

    [HttpGet("foods/{fdcId:int}")]
    [EnableRateLimiting(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
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
    [EnableRateLimiting(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
    [ProducesResponseType<DailyMicronutrientSummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetDailyMicronutrients(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime date) =>
        HandleOk(UsdaHttpMappings.ToDailyQuery(userId, date), static value => value.ToHttpResponse());
}
