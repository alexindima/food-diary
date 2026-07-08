using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class MediatorDashboardMealsReadService(ISender sender) : IDashboardMealsReadService {
    public async Task<Result<DashboardMealsReadModel>> GetMealsAsync(
        UserId userId,
        int page,
        int limit,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        Result<PagedResponse<ConsumptionModel>> result = await sender.Send(
            new GetConsumptionsQuery(userId.Value, page, limit, dateFrom, dateTo),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<DashboardMealsReadModel>(result.Error);
        }

        PagedResponse<ConsumptionModel> value = result.Value;
        return Result.Success(new DashboardMealsReadModel(
            [.. value.Data.Select(ToReadModel)],
            value.Page,
            value.Limit,
            value.TotalPages,
            value.TotalItems));
    }

    private static DashboardMealReadModel ToReadModel(ConsumptionModel meal) {
        return new DashboardMealReadModel(
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
            meal.IsFavorite,
            meal.FavoriteMealId,
            [.. meal.Items.Select(ToReadModel)],
            [.. meal.AiSessions.Select(ToReadModel)]);
    }

    private static DashboardMealItemReadModel ToReadModel(ConsumptionItemModel item) {
        return new DashboardMealItemReadModel(
            item.Id,
            item.ConsumptionId,
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
            item.ProductQualityScore,
            item.ProductQualityGrade,
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
            item.SourceAiItemId,
            item.Origin);
    }

    private static DashboardMealAiSessionReadModel ToReadModel(ConsumptionAiSessionModel session) {
        return new DashboardMealAiSessionReadModel(
            session.Id,
            session.ConsumptionId,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items.Select(ToReadModel)]);
    }

    private static DashboardMealAiItemReadModel ToReadModel(ConsumptionAiItemModel item) {
        return new DashboardMealAiItemReadModel(
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
