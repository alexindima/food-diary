using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Mappings;
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
    /// <summary>
    /// Получить все продукты (CQRS Query)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new
            {
                error = "Authentication.InvalidToken",
                message = "Не удалось определить пользователя"
            });
        }

        var sanitizedPage = Math.Max(page, 1);
        var sanitizedLimit = Math.Clamp(limit, 1, 100);
        var sanitizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = new GetProductsQuery(userId.Value, sanitizedPage, sanitizedLimit, sanitizedSearch);
        var result = await mediator.Send(query);
        return result.ToActionResult();
    }

    /// <summary>
    /// Создать новый продукт (CQRS Command)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new
            {
                error = "Authentication.InvalidToken",
                message = "Не удалось определить пользователя"
            });
        }

        var command = request.ToCommand(userId.Value);
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }
}
