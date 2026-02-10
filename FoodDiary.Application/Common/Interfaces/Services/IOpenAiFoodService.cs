using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IOpenAiFoodService
{
    Task<Result<FoodVisionResponse>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        UserId userId,
        string? description,
        CancellationToken cancellationToken);
    Task<Result<FoodNutritionResponse>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItem> items,
        UserId userId,
        CancellationToken cancellationToken);
}
