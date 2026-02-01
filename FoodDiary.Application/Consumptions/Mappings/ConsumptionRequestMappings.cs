using System.Linq;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Mappings;

public static class ConsumptionRequestMappings
{
    public static CreateConsumptionCommand ToCommand(this CreateConsumptionRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            request.Date,
            request.MealType,
            request.Comment,
            request.ImageUrl,
            request.ImageAssetId,
            request.Items.Select(ToInput).ToList(),
            request.IsNutritionAutoCalculated,
            request.ManualCalories,
            request.ManualProteins,
            request.ManualFats,
            request.ManualCarbs,
            request.ManualFiber,
            request.ManualAlcohol,
            request.PreMealSatietyLevel,
            request.PostMealSatietyLevel);

    public static UpdateConsumptionCommand ToCommand(this UpdateConsumptionRequest request, Guid? userId, Guid consumptionId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new MealId(consumptionId),
            request.Date,
            request.MealType,
            request.Comment,
            request.ImageUrl,
            request.ImageAssetId,
            request.Items.Select(ToInput).ToList(),
            request.IsNutritionAutoCalculated,
            request.ManualCalories,
            request.ManualProteins,
            request.ManualFats,
            request.ManualCarbs,
            request.ManualFiber,
            request.ManualAlcohol,
            request.PreMealSatietyLevel,
            request.PostMealSatietyLevel);

    private static ConsumptionItemInput ToInput(ConsumptionItemRequest request) =>
        new(request.ProductId, request.RecipeId, request.Amount);
}
