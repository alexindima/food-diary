using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IOpenAiFoodClient {
    Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> AnalyzeFoodImageAsync(
        string imageUrl,
        string? userLanguage,
        string? description,
        string promptTemplate,
        CancellationToken cancellationToken);

    Task<Result<OpenAiFoodClientResponse<FoodVisionModel>>> ParseFoodTextAsync(
        string text,
        string? userLanguage,
        string promptTemplate,
        CancellationToken cancellationToken);

    Task<Result<OpenAiFoodClientResponse<FoodNutritionModel>>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        string promptTemplate,
        CancellationToken cancellationToken);
}

public sealed record OpenAiFoodClientResponse<T>(
    T Value,
    string Operation,
    string Model,
    AiUsageTokens? Usage);

public readonly record struct AiUsageTokens(int InputTokens, int OutputTokens, int TotalTokens);
