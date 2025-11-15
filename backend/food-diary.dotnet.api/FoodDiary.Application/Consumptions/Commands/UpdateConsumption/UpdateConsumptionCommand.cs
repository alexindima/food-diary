using System;
using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

public record UpdateConsumptionCommand(
    UserId? UserId,
    int ConsumptionId,
    DateTime Date,
    string? MealType,
    string? Comment,
    IReadOnlyList<ConsumptionItemInput> Items,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber) : ICommand<Result<ConsumptionResponse>>;
