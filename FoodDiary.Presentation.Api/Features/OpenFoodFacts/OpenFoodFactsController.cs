using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.OpenFoodFacts.Mappings;
using FoodDiary.Presentation.Api.Features.OpenFoodFacts.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.OpenFoodFacts;

[ApiController]
[Route("api/v{version:apiVersion}/open-food-facts")]
public class OpenFoodFactsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("products/{barcode}")]
    [ProducesResponseType<OpenFoodFactsProductHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> SearchByBarcode(string barcode) =>
        HandleOk(OpenFoodFactsHttpMappings.ToQuery(barcode), static value => value.ToHttpResponse());

    [HttpGet("products")]
    [ProducesResponseType<IReadOnlyList<OpenFoodFactsProductHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Search(
        [FromQuery] string search,
        [FromQuery] int limit = 10) =>
        HandleOk(OpenFoodFactsHttpMappings.ToSearchQuery(search, limit), static value => value.ToListHttpResponse());
}
