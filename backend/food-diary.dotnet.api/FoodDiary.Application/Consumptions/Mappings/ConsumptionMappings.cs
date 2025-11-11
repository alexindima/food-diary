using System;
using System.Collections.Generic;
using System.Linq;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Consumptions.Mappings;

public static class ConsumptionMappings
{
    public static ConsumptionResponse ToResponse(this Meal meal)
    {
        var items = meal.Items
            .OrderBy(i => i.Id)
            .Select(item => new ConsumptionItemResponse(
                item.Id,
                item.MealId,
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
                item.RecipeId?.Value,
                item.Recipe?.Name,
                item.Recipe?.Servings,
                item.Recipe?.TotalCalories,
                item.Recipe?.TotalProteins,
                item.Recipe?.TotalFats,
                item.Recipe?.TotalCarbs,
                item.Recipe?.TotalFiber))
            .ToList();

        return new ConsumptionResponse(
            meal.Id,
            meal.Date,
            meal.MealType?.ToString(),
            meal.Comment,
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.TotalFiber,
            items);
    }

    public static PagedResponse<ConsumptionResponse> ToPagedResponse(
        this (IReadOnlyList<Meal> Items, int TotalItems) pageData,
        int page,
        int limit)
    {
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);
        var items = pageData.Items.Select(ToResponse).ToList();
        return new PagedResponse<ConsumptionResponse>(items, page, limit, totalPages, pageData.TotalItems);
    }
}
