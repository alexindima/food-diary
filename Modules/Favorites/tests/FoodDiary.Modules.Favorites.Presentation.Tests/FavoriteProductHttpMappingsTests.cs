using FoodDiary.Modules.Favorites.Presentation.Mappings.Features.FavoriteProducts.Mappings;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Mappings;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Requests;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteProducts.Responses;

namespace FoodDiary.Modules.Favorites.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteProductHttpMappingsTests {
    [Fact]
    public void AddFavoriteProductRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new AddFavoriteProductHttpRequest(productId, "Breakfast", PreferredPortionAmount: 120);

        AddFavoriteProductCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(productId, command.ProductId),
            () => Assert.Equal("Breakfast", command.Name),
            () => Assert.Equal(120, command.PreferredPortionAmount));
    }

    [Fact]
    public void UpdateFavoriteProductRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var favoriteProductId = Guid.NewGuid();
        var request = new UpdateFavoriteProductHttpRequest("Breakfast", PreferredPortionAmount: 150);

        UpdateFavoriteProductCommand command = request.ToCommand(userId, favoriteProductId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(favoriteProductId, command.FavoriteProductId),
            () => Assert.Equal("Breakfast", command.Name),
            () => Assert.Equal(150, command.PreferredPortionAmount));
    }

    [Fact]
    public void FavoriteProductId_ToQueriesAndDeleteCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var favoriteProductId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        RemoveFavoriteProductCommand delete = favoriteProductId.ToDeleteCommand(userId);
        GetFavoriteProductsQuery query = userId.ToQuery();
        IsProductFavoriteQuery favoriteQuery = productId.ToIsFavoriteQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, delete.UserId),
            () => Assert.Equal(favoriteProductId, delete.FavoriteProductId),
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(userId, favoriteQuery.UserId),
            () => Assert.Equal(productId, favoriteQuery.ProductId));
    }

    [Fact]
    public void FavoriteProductModel_ToHttpResponse_MapsAllFields() {
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        var model = new FavoriteProductModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Breakfast",
            createdAtUtc,
            "Milk",
            "Brand",
            Barcode: "1234567890123",
            Comment: "Fresh",
            ImageUrl: "https://cdn.example/milk.png",
            CaloriesPerBase: 60,
            ProteinsPerBase: 3,
            FatsPerBase: 2,
            CarbsPerBase: 5,
            FiberPerBase: 1,
            AlcoholPerBase: 0,
            QualityScore: 78,
            QualityGrade: "green",
            IsOwnedByCurrentUser: true,
            BaseUnit: "100g",
            PreferredPortionAmount: 150,
            DefaultPortionAmount: 100);

        FavoriteProductHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(model.Id, response.Id),
            () => Assert.Equal(model.ProductId, response.ProductId),
            () => Assert.Equal("Breakfast", response.Name),
            () => Assert.Equal(createdAtUtc, response.CreatedAtUtc),
            () => Assert.Equal("Milk", response.ProductName),
            () => Assert.Equal("Brand", response.Brand),
            () => Assert.Equal("1234567890123", response.Barcode),
            () => Assert.Equal("Fresh", response.Comment),
            () => Assert.Equal("https://cdn.example/milk.png", response.ImageUrl),
            () => Assert.Equal(60, response.CaloriesPerBase),
            () => Assert.Equal(3, response.ProteinsPerBase),
            () => Assert.Equal(2, response.FatsPerBase),
            () => Assert.Equal(5, response.CarbsPerBase),
            () => Assert.Equal(1, response.FiberPerBase),
            () => Assert.Equal(0, response.AlcoholPerBase),
            () => Assert.Equal(78, response.QualityScore),
            () => Assert.Equal("green", response.QualityGrade),
            () => Assert.True(response.IsOwnedByCurrentUser),
            () => Assert.Equal("100g", response.BaseUnit),
            () => Assert.Equal(150, response.PreferredPortionAmount),
            () => Assert.Equal(100, response.DefaultPortionAmount));
    }
}
