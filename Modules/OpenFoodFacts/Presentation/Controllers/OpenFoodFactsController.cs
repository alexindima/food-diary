using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.OpenFoodFacts.Presentation.Mappings;
using FoodDiary.Modules.OpenFoodFacts.Presentation.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.OpenFoodFacts.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/open-food-facts")]
[EnableRateLimiting(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
public sealed class OpenFoodFactsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("products/{barcode}")]
    [ProducesResponseType<OpenFoodFactsProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> SearchByBarcode(
        [Required, MaxLength(OpenFoodFactsRequestLimits.MaximumBarcodeLength)] string barcode) =>
        HandleOk(OpenFoodFactsHttpMappings.ToQuery(barcode), static value => value.ToHttpResponse());

    [HttpGet("products")]
    [ProducesResponseType<IReadOnlyList<OpenFoodFactsProductHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Search(
        [FromQuery, MaxLength(OpenFoodFactsRequestLimits.MaximumSearchLength)] string search,
        [FromQuery, Range(OpenFoodFactsRequestLimits.MinimumLimit, OpenFoodFactsRequestLimits.MaximumLimit)] int limit = 10) =>
        HandleOk(OpenFoodFactsHttpMappings.ToSearchQuery(search, limit), static value => value.ToListHttpResponse());
}
