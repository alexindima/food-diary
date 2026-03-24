using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Products.Mappings;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Products;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetProductsHttpQuery query) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("with-recent")]
    public async Task<IActionResult> GetAllWithRecent([FromQuery] GetProductsWithRecentHttpQuery query) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] GetRecentProductsHttpQuery query) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetProductByIdQuery(userId, new ProductId(id));
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new DeleteProductCommand(userId, new ProductId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new DuplicateProductCommand(userId, new ProductId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
