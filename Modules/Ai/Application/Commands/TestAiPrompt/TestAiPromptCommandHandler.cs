using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Prompts;
using FoodDiary.Modules.Ai.Contracts.Commands.TestAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.TestAiPrompt;

public sealed class TestAiPromptCommandHandler(IOpenAiFoodService foodService, IImageAssetContentService images)
    : IRequestHandler<TestAiPromptCommand, Result<string>> {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<Result<string>> Handle(TestAiPromptCommand request, CancellationToken cancellationToken) {
        AiPromptDraft draft = request.Draft;
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.RequestId)
            || !AiPromptDraftValidation.IsValid(draft, requireImage: true)) {
            return Result.Failure<string>(Errors.Validation.Invalid("Draft", "Invalid scenario, locale, prompt variables or sample."));
        }
        var userId = (UserId)request.UserId;
        var prompt = new AiPromptOverride(draft.PromptText, draft.Locale);
        if (string.Equals(draft.Key, "nutrition", StringComparison.Ordinal)) {
            Result<FoodNutritionModel> result = await foodService.CalculateNutritionAsync(draft.Items!, userId,
                request.RequestId, cancellationToken, prompt).ConfigureAwait(false);
            return Serialize(result);
        }
        if (string.Equals(draft.Key, "text-parse", StringComparison.Ordinal)) {
            Result<FoodVisionModel> result = await foodService.ParseFoodTextAsync(draft.Text!, userId,
                request.RequestId, cancellationToken, prompt).ConfigureAwait(false);
            return Serialize(result);
        }
        Result<string> image = await images.GetDataUrlAsync((ImageAssetId)draft.ImageAssetId!.Value, userId, cancellationToken).ConfigureAwait(false);
        if (image.IsFailure) {
            return Result.Failure<string>(image.Error);
        }
        return Serialize(await foodService.AnalyzeFoodImageAsync(image.Value, userId, draft.Text,
            request.RequestId, cancellationToken, prompt, product: string.Equals(draft.Key, "product-label", StringComparison.Ordinal) ? new ProductImageAnalysis([]) : null).ConfigureAwait(false));
    }

    private static Result<string> Serialize<T>(Result<T> result) => result.IsSuccess
        ? Result.Success(JsonSerializer.Serialize(result.Value, JsonOptions))
        : Result.Failure<string>(result.Error);
}
