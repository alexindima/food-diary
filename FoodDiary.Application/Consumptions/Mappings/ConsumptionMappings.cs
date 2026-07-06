using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Mappings;

public static class ConsumptionMappings {
    public static ConsumptionModel ToModel(
        this MealConsumptionReadModel meal,
        bool isOwnedByCurrentUser = true,
        bool isFavorite = false,
        Guid? favoriteMealId = null) {
        var items = meal.Items
            .OrderBy(static i => i.Id)
            .Select(ToItemModel)
            .ToList();

        var aiSessions = meal.AiSessions
            .OrderBy(static s => s.RecognizedAtUtc)
            .Select(ToAiSessionModel)
            .ToList();

        double effectiveCalories = meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;
        double effectiveProteins = meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;
        double effectiveFats = meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;
        double effectiveCarbs = meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;
        double effectiveFiber = meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;
        double effectiveAlcohol = meal.IsNutritionAutoCalculated ? meal.TotalAlcohol : meal.ManualAlcohol ?? meal.TotalAlcohol;
        var quality = FoodQualityScore.Calculate(
            effectiveCalories,
            effectiveProteins,
            effectiveFats,
            effectiveCarbs,
            effectiveFiber,
            effectiveAlcohol);

        return new ConsumptionModel(
            meal.Id,
            meal.Date,
            meal.MealType?.ToString(),
            isOwnedByCurrentUser ? meal.Comment : null,
            meal.ImageUrl,
            meal.ImageAssetId,
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

    public static ConsumptionModel ToModel(
        this Meal meal,
        bool isOwnedByCurrentUser = true,
        bool isFavorite = false,
        Guid? favoriteMealId = null) {
        var items = meal.Items
            .OrderBy(i => i.Id.Value)
            .Select(ToItemModel)
            .ToList();

        var aiSessions = meal.AiSessions
            .OrderBy(s => s.RecognizedAtUtc)
            .Select(ToAiSessionModel)
            .ToList();

        double effectiveCalories = meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;
        double effectiveProteins = meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;
        double effectiveFats = meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;
        double effectiveCarbs = meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;
        double effectiveFiber = meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;
        double effectiveAlcohol = meal.IsNutritionAutoCalculated ? meal.TotalAlcohol : meal.ManualAlcohol ?? meal.TotalAlcohol;
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

    private static ConsumptionItemModel ToItemModel(MealItem item) {
        FoodQualityScore? quality = item.Product?.GetQualityScore();
        bool hasSnapshot = item.HasNutritionSnapshot;
        return new ConsumptionItemModel(
            item.Id.Value,
            item.MealId.Value,
            item.Amount,
            item.ProductId?.Value,
            item.SnapshotName ?? item.Product?.Name,
            item.SnapshotImageUrl ?? item.Product?.ImageUrl,
            item.SnapshotUnit ?? item.Product?.BaseUnit.ToString(),
            item.SnapshotBaseAmount ?? item.Product?.BaseAmount,
            item.SnapshotCaloriesPerBase ?? item.Product?.CaloriesPerBase,
            item.SnapshotProteinsPerBase ?? item.Product?.ProteinsPerBase,
            item.SnapshotFatsPerBase ?? item.Product?.FatsPerBase,
            item.SnapshotCarbsPerBase ?? item.Product?.CarbsPerBase,
            item.SnapshotFiberPerBase ?? item.Product?.FiberPerBase,
            item.SnapshotAlcoholPerBase ?? item.Product?.AlcoholPerBase,
            item.RecipeId?.Value,
            item.SnapshotName ?? item.Recipe?.Name,
            item.SnapshotImageUrl ?? item.Recipe?.ImageUrl,
            hasSnapshot ? 1 : item.Recipe?.Servings,
            item.SnapshotCaloriesPerBase ?? item.Recipe?.TotalCalories,
            item.SnapshotProteinsPerBase ?? item.Recipe?.TotalProteins,
            item.SnapshotFatsPerBase ?? item.Recipe?.TotalFats,
            item.SnapshotCarbsPerBase ?? item.Recipe?.TotalCarbs,
            item.SnapshotFiberPerBase ?? item.Recipe?.TotalFiber,
            item.SnapshotAlcoholPerBase ?? item.Recipe?.TotalAlcohol,
            quality?.Score,
            quality?.Grade.ToString().ToLowerInvariant(),
            item.SourceAiItemId?.Value,
            item.Origin.ToString());
    }

    private static ConsumptionItemModel ToItemModel(MealConsumptionItemReadModel item) {
        FoodQualityScore? quality = item.ProductCaloriesPerBase.HasValue
            && item.ProductProteinsPerBase.HasValue
            && item.ProductFatsPerBase.HasValue
            && item.ProductCarbsPerBase.HasValue
            && item.ProductFiberPerBase.HasValue
            && item.ProductAlcoholPerBase.HasValue
            ? FoodQualityScore.Calculate(
                item.ProductCaloriesPerBase.Value,
                item.ProductProteinsPerBase.Value,
                item.ProductFatsPerBase.Value,
                item.ProductCarbsPerBase.Value,
                item.ProductFiberPerBase.Value,
                item.ProductAlcoholPerBase.Value,
                item.ProductType ?? ProductType.Unknown)
            : null;

        return new ConsumptionItemModel(
            item.Id,
            item.MealId,
            item.Amount,
            item.ProductId,
            item.ProductName,
            item.ProductImageUrl,
            item.ProductBaseUnit,
            item.ProductBaseAmount,
            item.ProductCaloriesPerBase,
            item.ProductProteinsPerBase,
            item.ProductFatsPerBase,
            item.ProductCarbsPerBase,
            item.ProductFiberPerBase,
            item.ProductAlcoholPerBase,
            item.RecipeId,
            item.RecipeName,
            item.RecipeImageUrl,
            item.RecipeServings,
            item.RecipeTotalCalories,
            item.RecipeTotalProteins,
            item.RecipeTotalFats,
            item.RecipeTotalCarbs,
            item.RecipeTotalFiber,
            item.RecipeTotalAlcohol,
            quality?.Score,
            quality?.Grade.ToString().ToLowerInvariant(),
            item.SourceAiItemId,
            item.Origin.ToString());
    }

    private static ConsumptionAiSessionModel ToAiSessionModel(MealAiSession session) {
        return new ConsumptionAiSessionModel(
            session.Id.Value,
            session.MealId.Value,
            session.ImageAssetId?.Value,
            session.ImageAsset?.Url,
            session.Source.ToString(),
            session.Status.ToString(),
            session.RecognizedAtUtc,
            session.Notes,
            session.Items.OrderBy(i => i.Id.Value).Select(ToAiItemModel).ToList());
    }

    private static ConsumptionAiSessionModel ToAiSessionModel(MealConsumptionAiSessionReadModel session) {
        return new ConsumptionAiSessionModel(
            session.Id,
            session.MealId,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source.ToString(),
            session.Status.ToString(),
            session.RecognizedAtUtc,
            session.Notes,
            session.Items.OrderBy(static i => i.Id).Select(ToAiItemModel).ToList());
    }

    private static ConsumptionAiItemModel ToAiItemModel(MealAiItem aiItem) {
        return new ConsumptionAiItemModel(
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
            aiItem.Alcohol,
            aiItem.Confidence,
            aiItem.Resolution.ToString());
    }

    private static ConsumptionAiItemModel ToAiItemModel(MealConsumptionAiItemReadModel aiItem) {
        return new ConsumptionAiItemModel(
            aiItem.Id,
            aiItem.SessionId,
            aiItem.NameEn,
            aiItem.NameLocal,
            aiItem.Amount,
            aiItem.Unit,
            aiItem.Calories,
            aiItem.Proteins,
            aiItem.Fats,
            aiItem.Carbs,
            aiItem.Fiber,
            aiItem.Alcohol,
            aiItem.Confidence,
            aiItem.Resolution.ToString());
    }

}
