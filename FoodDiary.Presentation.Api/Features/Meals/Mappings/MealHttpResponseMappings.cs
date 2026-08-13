using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Presentation.Api.Features.Meals.Responses;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Meals.Mappings;

public static class MealHttpResponseMappings {
    extension(MealModel model) {
        public MealHttpResponse ToHttpResponse() {
            return new MealHttpResponse(
                model.Id,
                model.Date,
                model.MealType,
                model.Comment,
                model.ImageUrl,
                model.ImageAssetId,
                model.TotalCalories,
                model.TotalProteins,
                model.TotalFats,
                model.TotalCarbs,
                model.TotalFiber,
                model.TotalAlcohol,
                model.IsNutritionAutoCalculated,
                model.ManualCalories,
                model.ManualProteins,
                model.ManualFats,
                model.ManualCarbs,
                model.ManualFiber,
                model.ManualAlcohol,
                model.PreMealSatietyLevel,
                model.PostMealSatietyLevel,
                model.QualityScore,
                model.QualityGrade,
                model.IsFavorite,
                model.FavoriteMealId,
                model.Items.ToHttpResponseList(ToHttpResponse),
                model.AiSessions.ToHttpResponseList(ToHttpResponse)
            );
        }
    }

    extension(PagedResponse<MealModel> response) {
        public PagedHttpResponse<MealHttpResponse> ToHttpResponse() {
            return response.ToPagedHttpResponse(ToHttpResponse);
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

    extension(MealItemModel model) {
        private MealItemHttpResponse ToHttpResponse() {
            return new MealItemHttpResponse(
                model.Id,
                model.MealId,
                model.Amount,
                model.ProductId,
                model.ProductName,
                model.ProductImageUrl,
                model.ProductBaseUnit,
                model.ProductBaseAmount,
                model.ProductCaloriesPerBase,
                model.ProductProteinsPerBase,
                model.ProductFatsPerBase,
                model.ProductCarbsPerBase,
                model.ProductFiberPerBase,
                model.ProductAlcoholPerBase,
                model.RecipeId,
                model.RecipeName,
                model.RecipeImageUrl,
                model.RecipeServings,
                model.RecipeTotalCalories,
                model.RecipeTotalProteins,
                model.RecipeTotalFats,
                model.RecipeTotalCarbs,
                model.RecipeTotalFiber,
                model.RecipeTotalAlcohol,
                model.ProductQualityScore,
                model.ProductQualityGrade,
                model.SourceAiItemId,
                model.Origin
            );
        }
    }

    extension(MealAiSessionModel model) {
        private MealAiSessionHttpResponse ToHttpResponse() {
            return new MealAiSessionHttpResponse(
                model.Id,
                model.MealId,
                model.ImageAssetId,
                model.ImageUrl,
                model.Source,
                model.Status,
                model.RecognizedAtUtc,
                model.Notes,
                model.Items.ToHttpResponseList(ToHttpResponse)
            );
        }
    }

    extension(MealAiItemModel model) {
        private MealAiItemHttpResponse ToHttpResponse() {
            return new MealAiItemHttpResponse(
                model.Id,
                model.SessionId,
                model.NameEn,
                model.NameLocal,
                model.Amount,
                model.Unit,
                model.Calories,
                model.Proteins,
                model.Fats,
                model.Carbs,
                model.Fiber,
                model.Alcohol,
                model.Confidence,
                model.Resolution
            );
        }
    }
}
