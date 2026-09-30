using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Application.Queries.GetPublicProduct;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Presentation.Responses;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Products.Presentation.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/products/public")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicProductsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PublicProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) => HandleOk(new GetPublicProductQuery(new ProductId(id)),
        static p => new PublicProductHttpResponse(p.Id, p.Name, p.Brand, p.ImageUrl, p.BaseUnit,
            p.BaseAmount, p.Calories, p.Proteins, p.Fats, p.Carbs, p.Fiber, p.Alcohol) {
            Description = p.Description,
            Images = p.Images,
        });
}
