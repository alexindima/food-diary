using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IOpenAiFoodService {
    Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        string? description,
        CancellationToken cancellationToken);

    Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        UserId userId,
        CancellationToken cancellationToken);

    Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        CancellationToken cancellationToken);
}
