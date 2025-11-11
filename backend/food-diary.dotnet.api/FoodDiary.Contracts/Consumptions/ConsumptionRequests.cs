using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Consumptions;

public record CreateConsumptionRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    IReadOnlyList<ConsumptionItemRequest> Items);

public record UpdateConsumptionRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    IReadOnlyList<ConsumptionItemRequest> Items);

public record ConsumptionItemRequest(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount);
