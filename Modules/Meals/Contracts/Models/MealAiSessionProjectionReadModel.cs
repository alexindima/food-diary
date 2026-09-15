using FoodDiary.Modules.Meals.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Meals.Contracts.Models;

public sealed record MealAiSessionProjectionReadModel(
    Guid Id,
    Guid MealId,
    Guid? ImageAssetId,
    string? ImageUrl,
    AiRecognitionSource Source,
    MealAiSessionStatus Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemProjectionReadModel> Items);
