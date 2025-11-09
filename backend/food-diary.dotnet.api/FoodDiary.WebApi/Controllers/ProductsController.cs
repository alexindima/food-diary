using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Contracts.Products;
using FoodDiary.WebApi.Extensions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includePublic = true)
    {
        var sanitizedPage = Math.Max(page, 1);
        var sanitizedLimit = Math.Clamp(limit, 1, 100);
        var sanitizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = new GetProductsQuery(User.GetUserId(), sanitizedPage, sanitizedLimit, sanitizedSearch, includePublic);
        var result = await mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetProductByIdQuery(User.GetUserId(), new ProductId(id));
        var result = await mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var command = request.ToCommand(User.GetUserId()?.Value);
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var command = request.ToCommand(User.GetUserId()?.Value, id);
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteProductCommand(User.GetUserId(), new ProductId(id));
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }
}
