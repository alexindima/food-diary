using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Meals.Queries.GetMeals;
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
        Result<PagedResponse<MealModel>> result = await sender.Send(
            new GetMealsQuery(userId.Value, page, limit, dateFrom, dateTo),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<DashboardMealsReadModel>(result.Error);
        }

        PagedResponse<MealModel> value = result.Value;
        return Result.Success(new DashboardMealsReadModel(
            [.. value.Data.Select(ToReadModel)],
            value.Page,
            value.Limit,
            value.TotalPages,
            value.TotalItems));
    }

    private static DashboardMealReadModel ToReadModel(MealModel meal) {
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

    private static DashboardMealItemReadModel ToReadModel(MealItemModel item) {
        return new DashboardMealItemReadModel(
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

    private static DashboardMealAiSessionReadModel ToReadModel(MealAiSessionModel session) {
        return new DashboardMealAiSessionReadModel(
            session.Id,
            session.MealId,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items.Select(ToReadModel)]);
    }

    private static DashboardMealAiItemReadModel ToReadModel(MealAiItemModel item) {
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
