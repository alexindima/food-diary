using FoodDiary.Modules.Favorites.Presentation.Mappings.Features.FavoriteProducts.Mappings;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Requests;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteProducts.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/favorite-products")]
public sealed class FavoriteProductsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<FavoriteProductHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpGet("page")]
    [ProducesResponseType<PagedHttpResponse<FavoriteProductHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPage([FromCurrentUser] Guid userId, [FromQuery] GetFavoriteProductPageHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => new PagedHttpResponse<FavoriteProductHttpResponse>(
            value.Data.Select(item => item.ToHttpResponse()).ToArray(), value.Page, value.Limit, value.TotalPages, value.TotalItems));

    [HttpGet("check/{productId:guid}")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public Task<IActionResult> IsFavorite(Guid productId, [FromCurrentUser] Guid userId) =>
        HandleOk(productId.ToIsFavoriteQuery(userId), static value => value);

    [HttpPost]
    [ProducesResponseType<FavoriteProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Add([FromCurrentUser] Guid userId, [FromBody] AddFavoriteProductHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<FavoriteProductHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateFavoriteProductHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Remove(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}
