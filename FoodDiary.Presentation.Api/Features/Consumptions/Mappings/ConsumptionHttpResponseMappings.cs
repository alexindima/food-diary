using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Presentation.Api.Features.Consumptions.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Mappings;

public static class ConsumptionHttpResponseMappings {
    public static ConsumptionHttpResponse ToHttpResponse(this ConsumptionModel model) {
        return new ConsumptionHttpResponse(
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
            model.Items.ToHttpResponseList(ToHttpResponse),
            model.AiSessions.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static PagedHttpResponse<ConsumptionHttpResponse> ToHttpResponse(this PagedResponse<ConsumptionModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    private static ConsumptionItemHttpResponse ToHttpResponse(this ConsumptionItemModel model) {
        return new ConsumptionItemHttpResponse(
            model.Id,
            model.ConsumptionId,
            model.Amount,
            model.ProductId,
            model.ProductName,
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
            model.RecipeServings,
            model.RecipeTotalCalories,
            model.RecipeTotalProteins,
            model.RecipeTotalFats,
            model.RecipeTotalCarbs,
            model.RecipeTotalFiber,
            model.RecipeTotalAlcohol,
            model.ProductQualityScore,
            model.ProductQualityGrade
        );
    }

    private static ConsumptionAiSessionHttpResponse ToHttpResponse(this ConsumptionAiSessionModel model) {
        return new ConsumptionAiSessionHttpResponse(
            model.Id,
            model.ConsumptionId,
            model.ImageAssetId,
            model.ImageUrl,
            model.Source,
            model.RecognizedAtUtc,
            model.Notes,
            model.Items.ToHttpResponseList(ToHttpResponse)
        );
    }

    private static ConsumptionAiItemHttpResponse ToHttpResponse(this ConsumptionAiItemModel model) {
        return new ConsumptionAiItemHttpResponse(
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
            model.Alcohol
        );
    }
}
