using MediatR;
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
public class ProductsController(ISender mediator) : ControllerBase {
    /// <summary>
    /// Получить все продукты (CQRS Query)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid userId format");

        var query = new GetProductsQuery(new UserId(userGuid));
        var result = await mediator.Send(query);
        return result.ToActionResult();
    }

    /// <summary>
    /// Создать новый продукт (CQRS Command)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, [FromQuery] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest("Invalid userId format");

        var command = request.ToCommand(userGuid);
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }

    // TODO: Добавить GetById, Update, Delete команды/запросы
    // TODO: Добавить [Authorize] и извлекать userId из JWT claims
}
