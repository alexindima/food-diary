using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Meals.Models;

public sealed record MealConsumptionAiSessionReadModel(
    Guid Id,
    Guid MealId,
    Guid? ImageAssetId,
    string? ImageUrl,
    AiRecognitionSource Source,
    MealAiSessionStatus Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealConsumptionAiItemReadModel> Items);
