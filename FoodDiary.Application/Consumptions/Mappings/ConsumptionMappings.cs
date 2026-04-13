using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Mappings;

public static class ConsumptionMappings {
    public static ConsumptionModel ToModel(
        this Meal meal,
        bool isOwnedByCurrentUser = true,
        bool isFavorite = false,
        Guid? favoriteMealId = null) {
        var items = meal.Items
            .OrderBy(i => i.Id.Value)
            .Select(item => {
                var quality = item.Product?.GetQualityScore();
                return new ConsumptionItemModel(
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
                    item.Recipe?.TotalAlcohol,
                    quality?.Score,
                    quality?.Grade.ToString().ToLowerInvariant());
            })
            .ToList();

        var aiSessions = meal.AiSessions
            .OrderBy(s => s.RecognizedAtUtc)
            .Select(session => new ConsumptionAiSessionModel(
                session.Id.Value,
                session.MealId.Value,
                session.ImageAssetId?.Value,
                session.ImageAsset?.Url,
                session.Source.ToString(),
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

        var effectiveCalories = meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;
        var effectiveProteins = meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;
        var effectiveFats = meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;
        var effectiveCarbs = meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;
        var effectiveFiber = meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;
        var effectiveAlcohol = meal.IsNutritionAutoCalculated ? meal.TotalAlcohol : meal.ManualAlcohol ?? meal.TotalAlcohol;
        var quality = FoodQualityScore.Calculate(
            effectiveCalories,
            effectiveProteins,
            effectiveFats,
            effectiveCarbs,
            effectiveFiber,
            effectiveAlcohol);

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
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            isFavorite,
            favoriteMealId,
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
