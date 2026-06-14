using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.FavoriteProducts;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteProductsControllerTests {
    [Fact]
    public async Task GetAll_SendsQueryAndReturnsFavorites() {
        FavoriteProductModel favorite = CreateFavorite();
        IRequest<Result<IReadOnlyList<FavoriteProductModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<FavoriteProductModel>>([favorite]), request => sentRequest = request);
        FavoriteProductsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetAll(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<FavoriteProductHttpResponse> response = Assert.IsType<List<FavoriteProductHttpResponse>>(ok.Value);
        Assert.Single(response);
        Assert.Equal(favorite.Id, response[0].Id);
        GetFavoriteProductsQuery query = Assert.IsType<GetFavoriteProductsQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task IsFavorite_SendsQueryAndReturnsFlag() {
        IRequest<Result<bool>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(value: true), request => sentRequest = request);
        FavoriteProductsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        IActionResult result = await controller.IsFavorite(productId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
        IsProductFavoriteQuery query = Assert.IsType<IsProductFavoriteQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(productId, query.ProductId);
    }

    [Fact]
    public async Task Add_SendsCommandAndReturnsFavorite() {
        FavoriteProductModel favorite = CreateFavorite();
        IRequest<Result<FavoriteProductModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(favorite), request => sentRequest = request);
        FavoriteProductsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new AddFavoriteProductHttpRequest(productId, "Breakfast", PreferredPortionAmount: 120);

        IActionResult result = await controller.Add(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        FavoriteProductHttpResponse response = Assert.IsType<FavoriteProductHttpResponse>(ok.Value);
        Assert.Equal(favorite.Id, response.Id);
        AddFavoriteProductCommand command = Assert.IsType<AddFavoriteProductCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(productId, command.ProductId);
        Assert.Equal("Breakfast", command.Name);
        Assert.Equal(120, command.PreferredPortionAmount);
    }

    [Fact]
    public async Task Update_SendsCommandAndReturnsFavorite() {
        FavoriteProductModel favorite = CreateFavorite();
        IRequest<Result<FavoriteProductModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(favorite), request => sentRequest = request);
        FavoriteProductsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var favoriteProductId = Guid.NewGuid();
        var request = new UpdateFavoriteProductHttpRequest("Breakfast", PreferredPortionAmount: 150);

        IActionResult result = await controller.Update(favoriteProductId, userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        FavoriteProductHttpResponse response = Assert.IsType<FavoriteProductHttpResponse>(ok.Value);
        Assert.Equal(favorite.Id, response.Id);
        UpdateFavoriteProductCommand command = Assert.IsType<UpdateFavoriteProductCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(favoriteProductId, command.FavoriteProductId);
        Assert.Equal("Breakfast", command.Name);
        Assert.Equal(150, command.PreferredPortionAmount);
    }

    [Fact]
    public async Task Remove_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        FavoriteProductsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var favoriteProductId = Guid.NewGuid();

        IActionResult result = await controller.Remove(favoriteProductId, userId);

        Assert.IsType<NoContentResult>(result);
        RemoveFavoriteProductCommand command = Assert.IsType<RemoveFavoriteProductCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(favoriteProductId, command.FavoriteProductId);
    }

    private static FavoriteProductsController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static FavoriteProductModel CreateFavorite() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Breakfast",
            DateTime.UtcNow.AddDays(-1),
            "Milk",
            "Brand",
            "https://cdn.example/milk.png",
            CaloriesPerBase: 60,
            BaseUnit: "100g",
            PreferredPortionAmount: 150,
            DefaultPortionAmount: 100);
}
