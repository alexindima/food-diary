using System;
using System.Collections.Generic;

namespace FoodDiary.Application.Consumptions.Common;

public record ConsumptionAiSessionInput(
    Guid? ImageAssetId,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemInput> Items);

public record ConsumptionAiItemInput(
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
