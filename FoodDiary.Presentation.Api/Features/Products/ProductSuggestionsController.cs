using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Products.Mappings;
using FoodDiary.Presentation.Api.Features.Products.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Products;

[ApiController]
[Route("api/v{version:apiVersion}/products/suggestions")]
[EnableRateLimiting(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
public sealed class ProductSuggestionsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductSearchSuggestionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> SearchSuggestions(
        [FromQuery] string search,
        [FromQuery] int limit = 5) =>
        HandleOk(ProductHttpMappings.ToSuggestionsQuery(search, limit), static value => value.ToHttpResponse());
}
