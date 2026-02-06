using System;
using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public record CreateConsumptionCommand(
    UserId? UserId,
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<ConsumptionItemInput> Items,
    IReadOnlyList<ConsumptionAiSessionInput> AiSessions,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel) : ICommand<Result<ConsumptionResponse>>;
