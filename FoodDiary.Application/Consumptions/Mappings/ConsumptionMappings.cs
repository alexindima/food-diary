using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Consumptions.Mappings;

public static class ConsumptionMappings {
    public static ConsumptionModel ToModel(this Meal meal, bool isOwnedByCurrentUser = true) {
        var items = meal.Items
            .OrderBy(i => i.Id.Value)
            .Select(item => new ConsumptionItemModel(
                item.Id.Value,
                item.MealId.Value,
                item.Amount,
                item.ProductId?.Value,
                item.Product?.Name,
                item.Product?.BaseUnit.ToString(),
                item.Product?.BaseAmount,
                item.Product?.CaloriesPerBase,
                item.Product?.ProteinsPerBase,
                item.Product?.FatsPerBase,
                item.Product?.CarbsPerBase,
                item.Product?.FiberPerBase,
                item.Product?.AlcoholPerBase,
                item.RecipeId?.Value,
                item.Recipe?.Name,
                item.Recipe?.Servings,
                item.Recipe?.TotalCalories,
                item.Recipe?.TotalProteins,
                item.Recipe?.TotalFats,
                item.Recipe?.TotalCarbs,
                item.Recipe?.TotalFiber,
                item.Recipe?.TotalAlcohol))
            .ToList();

        var aiSessions = meal.AiSessions
            .OrderBy(s => s.RecognizedAtUtc)
            .Select(session => new ConsumptionAiSessionModel(
                session.Id.Value,
                session.MealId.Value,
                session.ImageAssetId?.Value,
                session.ImageAsset?.Url,
                session.RecognizedAtUtc,
                session.Notes,
                session.Items
                    .OrderBy(i => i.Id.Value)
                    .Select(aiItem => new ConsumptionAiItemModel(
                        aiItem.Id.Value,
                        aiItem.MealAiSessionId.Value,
                        aiItem.NameEn,
                        aiItem.NameLocal,
                        aiItem.Amount,
                        aiItem.Unit,
                        aiItem.Calories,
                        aiItem.Proteins,
                        aiItem.Fats,
                        aiItem.Carbs,
                        aiItem.Fiber,
                        aiItem.Alcohol))
                    .ToList()))
            .ToList();

        return new ConsumptionModel(
            meal.Id.Value,
            meal.Date,
            meal.MealType?.ToString(),
            isOwnedByCurrentUser ? meal.Comment : null,
            meal.ImageUrl,
            meal.ImageAssetId?.Value,
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.TotalFiber,
            meal.TotalAlcohol,
            meal.IsNutritionAutoCalculated,
            meal.ManualCalories,
            meal.ManualProteins,
            meal.ManualFats,
            meal.ManualCarbs,
            meal.ManualFiber,
            meal.ManualAlcohol,
            meal.PreMealSatietyLevel,
            meal.PostMealSatietyLevel,
            items,
            aiSessions);
    }

    public static PagedResponse<ConsumptionModel> ToPagedResponse(
        this (IReadOnlyList<Meal> Items, int TotalItems) pageData,
        int page,
        int limit) {
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);
        var items = pageData.Items.Select(item => item.ToModel()).ToList();
        return new PagedResponse<ConsumptionModel>(items, page, limit, totalPages, pageData.TotalItems);
    }
}
