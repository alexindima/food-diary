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
    public async Task<IActionResult> GetAll([FromCurrentUser] UserId userId, [FromQuery] GetProductsHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpGet("with-recent")]
    public async Task<IActionResult> GetAllWithRecent([FromCurrentUser] UserId userId, [FromQuery] GetProductsWithRecentHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromCurrentUser] UserId userId, [FromQuery] GetRecentProductsHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.IsSuccess
            ? Ok(result.Value.Select(x => x.ToHttpResponse()).ToList())
            : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] UserId userId) {
        var query = new GetProductByIdQuery(userId, new ProductId(id));
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateProductHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateProductHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DeleteProductCommand(userId, new ProductId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DuplicateProductCommand(userId, new ProductId(id));
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }
}
