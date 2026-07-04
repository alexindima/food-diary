using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Dashboard.Services;

internal static class DashboardMealsMapper {
    public static DashboardMealsModel ToModel(DashboardMealsReadModel response) {
        return new DashboardMealsModel(
            [.. response.Items.Select(ToConsumptionModel)],
            response.TotalItems);
    }

    private static ConsumptionModel ToConsumptionModel(DashboardMealReadModel meal) {
        FoodQualityScore quality = CalculateMealQuality(meal);

        return new ConsumptionModel(
            meal.Id,
            meal.Date,
            meal.MealType,
            meal.Comment,
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
            meal.IsFavorite,
            meal.FavoriteMealId,
            [.. meal.Items.OrderBy(item => item.Id).Select(ToConsumptionItemModel)],
            [.. meal.AiSessions.OrderBy(session => session.RecognizedAtUtc).Select(ToConsumptionAiSessionModel)]);
    }

    private static FoodQualityScore CalculateMealQuality(DashboardMealReadModel meal) {
        double effectiveCalories = meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;
        double effectiveProteins = meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;
        double effectiveFats = meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;
        double effectiveCarbs = meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;
        double effectiveFiber = meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;
        double effectiveAlcohol = meal.IsNutritionAutoCalculated ? meal.TotalAlcohol : meal.ManualAlcohol ?? meal.TotalAlcohol;

        return FoodQualityScore.Calculate(
            effectiveCalories,
            effectiveProteins,
            effectiveFats,
            effectiveCarbs,
            effectiveFiber,
            effectiveAlcohol);
    }

    private static ConsumptionItemModel ToConsumptionItemModel(DashboardMealItemReadModel item) {
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
            item.ProductQualityScore,
            item.ProductQualityGrade,
            item.SourceAiItemId,
            item.Origin);
    }

    private static ConsumptionAiSessionModel ToConsumptionAiSessionModel(DashboardMealAiSessionReadModel session) {
        return new ConsumptionAiSessionModel(
            session.Id,
            session.MealId,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items.OrderBy(item => item.Id).Select(ToConsumptionAiItemModel)]);
    }

    private static ConsumptionAiItemModel ToConsumptionAiItemModel(DashboardMealAiItemReadModel item) {
        return new ConsumptionAiItemModel(
            item.Id,
            item.SessionId,
            item.NameEn,
            item.NameLocal,
            item.Amount,
            item.Unit,
            item.Calories,
            item.Proteins,
            item.Fats,
            item.Carbs,
            item.Fiber,
            item.Alcohol,
            item.Confidence,
            item.Resolution);
    }
}
