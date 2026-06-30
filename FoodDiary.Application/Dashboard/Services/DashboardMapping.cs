using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Dashboard.Services;

public static class DashboardMapping {
    public static DashboardStatisticsModel ToStatisticsModel(DashboardStatisticsBucketReadModel? response, User? user) {
        if (response is null) {
            return new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null);
        }

        return new DashboardStatisticsModel(
            response.TotalCalories,
            response.AverageProteins,
            response.AverageFats,
            response.AverageCarbs,
            response.AverageFiber,
            user?.ProteinTarget,
            user?.FatTarget,
            user?.CarbTarget,
            user?.FiberTarget);
    }

    public static DashboardStatisticsModel ToStatisticsModel(AggregatedStatisticsModel? response, User? user) {
        if (response is null) {
            return new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null);
        }

        return new DashboardStatisticsModel(
            response.TotalCalories,
            response.AverageProteins,
            response.AverageFats,
            response.AverageCarbs,
            response.AverageFiber,
            user?.ProteinTarget,
            user?.FatTarget,
            user?.CarbTarget,
            user?.FiberTarget);
    }

    public static IReadOnlyList<DailyCaloriesModel> ToWeeklyCalories(IReadOnlyList<AggregatedStatisticsModel> responses) {
        return responses
            .OrderBy(r => r.DateFrom)
            .Select(r => new DailyCaloriesModel(r.DateFrom, r.TotalCalories))
            .ToList();
    }

    public static IReadOnlyList<DailyCaloriesModel> ToWeeklyCalories(IReadOnlyList<DashboardStatisticsBucketReadModel> responses) {
        return responses
            .OrderBy(r => r.DateFrom)
            .Select(r => new DailyCaloriesModel(r.DateFrom, r.TotalCalories))
            .ToList();
    }

    public static DashboardWeightModel ToWeightModel(IReadOnlyList<WeightEntry> entries, double? desired) {
        WeightEntry? latest = entries.Count > 0 ? entries[0] : null;
        WeightEntry? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWeightModel(
            latest is null ? null : new WeightPointModel(latest.Date, latest.Weight),
            previous is null ? null : new WeightPointModel(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWeightModel ToWeightModel(IReadOnlyList<DashboardWeightPointReadModel> entries, double? desired) {
        DashboardWeightPointReadModel? latest = entries.Count > 0 ? entries[0] : null;
        DashboardWeightPointReadModel? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWeightModel(
            latest is null ? null : new WeightPointModel(latest.Date, latest.Weight),
            previous is null ? null : new WeightPointModel(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<WaistEntry> entries, double? desired) {
        WaistEntry? latest = entries.Count > 0 ? entries[0] : null;
        WaistEntry? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWaistModel(
            latest is null ? null : new WaistPointModel(latest.Date, latest.Circumference),
            previous is null ? null : new WaistPointModel(previous.Date, previous.Circumference),
            desired);
    }

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<DashboardWaistPointReadModel> entries, double? desired) {
        DashboardWaistPointReadModel? latest = entries.Count > 0 ? entries[0] : null;
        DashboardWaistPointReadModel? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWaistModel(
            latest is null ? null : new WaistPointModel(latest.Date, latest.Circumference),
            previous is null ? null : new WaistPointModel(previous.Date, previous.Circumference),
            desired);
    }

    public static IReadOnlyList<WeightEntrySummaryModel> ToWeightTrend(IReadOnlyList<DashboardWeightSummaryReadModel> responses) {
        return responses
            .OrderBy(response => response.DateFrom)
            .Select(response => new WeightEntrySummaryModel(response.DateFrom, response.DateTo, response.AverageWeight))
            .ToList();
    }

    public static IReadOnlyList<WaistEntrySummaryModel> ToWaistTrend(IReadOnlyList<DashboardWaistSummaryReadModel> responses) {
        return responses
            .OrderBy(response => response.DateFrom)
            .Select(response => new WaistEntrySummaryModel(response.DateFrom, response.DateTo, response.AverageCircumference))
            .ToList();
    }

    public static DashboardMealsModel ToMealsModel(DashboardMealsReadModel response) {
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
