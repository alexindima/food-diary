using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Mappings;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class FavoriteMealHttpMappingsTests {
    [Fact]
    public void AddFavoriteMealRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var request = new AddFavoriteMealHttpRequest(mealId, "My favorite breakfast");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(mealId, command.MealId);
        Assert.Equal("My favorite breakfast", command.Name);
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndFavoriteId() {
        var userId = Guid.NewGuid();
        var favoriteId = Guid.NewGuid();

        var command = favoriteId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(favoriteId, command.FavoriteMealId);
    }

    [Fact]
    public void ToQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToIsFavoriteQuery_MapsUserIdAndMealId() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();

        var query = mealId.ToIsFavoriteQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(mealId, query.MealId);
    }

    [Fact]
    public void FavoriteMealModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var mealDate = DateTime.UtcNow.Date;
        var model = new FavoriteMealModel(id, mealId, "Breakfast", createdAt, mealDate, "Breakfast", 450, 30, 15, 55, 3);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(mealId, response.MealId);
        Assert.Equal("Breakfast", response.Name);
        Assert.Equal(createdAt, response.CreatedAtUtc);
        Assert.Equal(mealDate, response.MealDate);
        Assert.Equal("Breakfast", response.MealType);
        Assert.Equal(450, response.TotalCalories);
        Assert.Equal(30, response.TotalProteins);
        Assert.Equal(15, response.TotalFats);
        Assert.Equal(55, response.TotalCarbs);
        Assert.Equal(3, response.ItemCount);
    }
}
