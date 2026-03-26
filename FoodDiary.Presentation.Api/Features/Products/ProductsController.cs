using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Products.Mappings;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Products.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Products;

[ApiController]
[Route("api/products")]
public class ProductsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<ProductHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetProductsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("with-recent")]
    [ProducesResponseType<ProductListWithRecentHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAllWithRecent([FromCurrentUser] Guid userId, [FromQuery] GetProductsWithRecentHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("recent")]
    [ProducesResponseType<List<ProductHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetRecent([FromCurrentUser] Guid userId, [FromQuery] GetRecentProductsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetById(Guid id, [FromCurrentUser] Guid userId) =>
        HandleOk(id.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<ProductHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateProductHttpRequest request) =>
        HandleCreated(
            request.ToCommand(userId),
            nameof(GetById),
            static value => new { id = value.Id },
            static value => value.ToHttpResponse());

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<ProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateProductHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));

    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType<ProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Duplicate(Guid id, [FromCurrentUser] Guid userId) =>
        HandleOk(id.ToDuplicateCommand(userId), static value => value.ToHttpResponse());
}

