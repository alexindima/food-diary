using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Ai;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IOpenAiFoodService
{
    Task<Result<FoodVisionResponse>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        CancellationToken cancellationToken);
    Task<Result<FoodNutritionResponse>> CalculateNutritionAsync(IReadOnlyList<FoodVisionItem> items, CancellationToken cancellationToken);
}
