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
            request.Items.Select(ToInput).ToList());

    public static UpdateConsumptionCommand ToCommand(this UpdateConsumptionRequest request, Guid? userId, int consumptionId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            consumptionId,
            request.Date,
            request.MealType,
            request.Comment,
            request.Items.Select(ToInput).ToList());

    private static ConsumptionItemInput ToInput(ConsumptionItemRequest request) =>
        new(request.ProductId, request.RecipeId, request.Amount);
}
