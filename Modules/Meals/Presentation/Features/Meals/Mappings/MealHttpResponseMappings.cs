using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Presentation.Api.Features.Meals.Responses;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Meals.Mappings;

public static class MealHttpResponseMappings {
    extension(PagedResponse<MealModel> response) {
        public PagedHttpResponse<MealHttpResponse> ToHttpResponse() {
            return response.ToPagedHttpResponse(MealSharedHttpResponseMappings.ToHttpResponse);
        }
    }

    extension(MealOverviewModel model) {
        public MealOverviewHttpResponse ToHttpResponse() {
            return new MealOverviewHttpResponse(
                model.AllMeals.ToHttpResponse(),
                model.FavoriteItems.Select(ToHttpResponse).ToList(),
                model.FavoriteTotalCount
            );
        }
    }

    private static FavoriteMealHttpResponse ToHttpResponse(MealFavoriteMealModel model) =>
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
