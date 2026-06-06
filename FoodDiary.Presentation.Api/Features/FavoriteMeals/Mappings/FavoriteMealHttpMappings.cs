using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;
using FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;

namespace FoodDiary.Presentation.Api.Features.FavoriteMeals.Mappings;

public static class FavoriteMealHttpMappings {
    public static AddFavoriteMealCommand ToCommand(this AddFavoriteMealHttpRequest request, Guid userId) =>
        new(userId, request.MealId, request.Name);

    extension(Guid id) {
        public RemoveFavoriteMealCommand ToDeleteCommand(Guid userId) =>
            new(userId, id);
        public GetFavoriteMealsQuery ToQuery() =>
            new(id);
        public IsMealFavoriteQuery ToIsFavoriteQuery(Guid userId) =>
            new(userId, id);
    }

    public static FavoriteMealHttpResponse ToHttpResponse(this FavoriteMealModel model) =>
        new(
            model.Id,
            model.MealId,
            model.Name,
            model.CreatedAtUtc,
            model.MealDate,
            model.MealType,
            model.TotalCalories,
            model.TotalProteins,
            model.TotalFats,
            model.TotalCarbs,
            model.ItemCount);
}
