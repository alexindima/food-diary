using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IOpenAiFoodService {
    Task<Result<RecipeImportDraftModel>> ImportRecipeVideoAsync(
        Stream? video, string? sourceUrl, string? text, UserId userId, string requestId, CancellationToken cancellationToken);

    Task<Result<RecipeImportDraftModel>> ImportRecipeAsync(
        string? sourceUrl, string? text, UserId userId, string requestId, CancellationToken cancellationToken);

    Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
        string imageUrl,
        UserId userId,
        string? description,
        string requestId,
        CancellationToken cancellationToken,
        AiPromptOverride? promptOverride = null, ProductImageAnalysis? product = null);

    Task<Result<FoodVisionModel>> ParseFoodTextAsync(
        string text,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken,
        AiPromptOverride? promptOverride = null);

    Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
        IReadOnlyList<FoodVisionItemModel> items,
        UserId userId,
        string requestId,
        CancellationToken cancellationToken,
        AiPromptOverride? promptOverride = null);
}
