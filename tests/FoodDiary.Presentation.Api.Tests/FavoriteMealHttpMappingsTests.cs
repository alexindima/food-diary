using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;
using FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Mappings;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteMealHttpMappingsTests {
    [Fact]
    public void AddFavoriteMealRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var request = new AddFavoriteMealHttpRequest(mealId, "My favorite breakfast");

        AddFavoriteMealCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(mealId, command.MealId),
            () => Assert.Equal("My favorite breakfast", command.Name));
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndFavoriteId() {
        var userId = Guid.NewGuid();
        var favoriteId = Guid.NewGuid();

        RemoveFavoriteMealCommand command = favoriteId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(favoriteId, command.FavoriteMealId);
    }

    [Fact]
    public void ToQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetFavoriteMealsQuery query = userId.ToQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToIsFavoriteQuery_MapsUserIdAndMealId() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();

        IsMealFavoriteQuery query = mealId.ToIsFavoriteQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(mealId, query.MealId);
    }

    [Fact]
    public void FavoriteMealModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        DateTime createdAt = DateTime.UtcNow;
        DateTime mealDate = DateTime.UtcNow.Date;
        var model = new FavoriteMealModel(id, mealId, "Breakfast", createdAt, mealDate, "Breakfast", 450, 30, 15, 55, 3);

        FavoriteMealHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal(mealId, response.MealId),
            () => Assert.Equal("Breakfast", response.Name),
            () => Assert.Equal(createdAt, response.CreatedAtUtc),
            () => Assert.Equal(mealDate, response.MealDate),
            () => Assert.Equal("Breakfast", response.MealType),
            () => Assert.Equal(450, response.TotalCalories),
            () => Assert.Equal(30, response.TotalProteins),
            () => Assert.Equal(15, response.TotalFats),
            () => Assert.Equal(55, response.TotalCarbs),
            () => Assert.Equal(3, response.ItemCount));
    }
}
