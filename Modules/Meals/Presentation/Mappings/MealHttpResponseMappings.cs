using FoodDiary.Modules.Meals.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Modules.Meals.Presentation.Contracts.Responses;
using FoodDiary.Modules.Meals.Presentation.Responses;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Meals.Presentation.Mappings;

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
            ) { DaySummaries = model.DaySummaries.Select(day => new MealDaySummaryHttpResponse(day.Date, day.TotalCalories, day.MealCount)).ToList() };
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
