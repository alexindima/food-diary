using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IOpenAiFoodService {
    Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        UserId userId,
        string? description,
        string requestId,
        CancellationToken cancellationToken);

    Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken);

    Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken);
}
