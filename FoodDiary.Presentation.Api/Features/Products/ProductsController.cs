using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsWithRecent;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
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
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includePublic = true,
        [FromQuery] string? productTypes = null) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var sanitizedPage = Math.Max(page, 1);
        var sanitizedLimit = Math.Clamp(limit, 1, 100);
        var sanitizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var sanitizedProductTypes = ParseProductTypes(productTypes);

        var query = new GetProductsQuery(userId, sanitizedPage, sanitizedLimit, sanitizedSearch,
            includePublic, sanitizedProductTypes);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("with-recent")]
    public async Task<IActionResult> GetAllWithRecent(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] int recentLimit = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includePublic = true,
        [FromQuery] string? productTypes = null) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var sanitizedPage = Math.Max(page, 1);
        var sanitizedLimit = Math.Clamp(limit, 1, 100);
        var sanitizedRecentLimit = Math.Clamp(recentLimit, 1, 50);
        var sanitizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var sanitizedProductTypes = ParseProductTypes(productTypes);

        var query = new GetProductsWithRecentQuery(
            userId,
            sanitizedPage,
            sanitizedLimit,
            sanitizedSearch,
            includePublic,
            sanitizedRecentLimit,
            sanitizedProductTypes);

        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent(
        [FromQuery] int limit = 10,
        [FromQuery] bool includePublic = true) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var sanitizedLimit = Math.Clamp(limit, 1, 50);
        var query = new GetRecentProductsQuery(userId, sanitizedLimit, includePublic);
        var result = await Mediator.Send(query);
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

    private static IReadOnlyCollection<string>? ParseProductTypes(string? productTypes) {
        if (string.IsNullOrWhiteSpace(productTypes)) {
            return null;
        }

        var values = productTypes
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length > 0 ? values : null;
    }
}
