using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includePublic = true) {
        var sanitizedPage = Math.Max(page, 1);
        var sanitizedLimit = Math.Clamp(limit, 1, 100);
        var sanitizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = new GetProductsQuery(CurrentUserId, sanitizedPage, sanitizedLimit, sanitizedSearch,
            includePublic);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) {
        var query = new GetProductByIdQuery(CurrentUserId, new ProductId(id));
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request) {
        var command = request.ToCommand(CurrentUserGuid);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request) {
        var command = request.ToCommand(CurrentUserGuid, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) {
        var command = new DeleteProductCommand(CurrentUserId, new ProductId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
