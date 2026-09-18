using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Prompts;
using FoodDiary.Modules.Ai.Contracts.Queries.PreviewAiPrompt;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.PreviewAiPrompt;

public sealed class PreviewAiPromptQueryHandler(IAiPromptPreviewRenderer renderer)
    : IRequestHandler<PreviewAiPromptQuery, Result<string>> {
    public Task<Result<string>> Handle(PreviewAiPromptQuery request, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AiPromptDraftValidation.IsValid(request.Draft, requireImage: false)
            ? Result.Success(renderer.Render(request.Draft))
            : Result.Failure<string>(Errors.Validation.Invalid("Draft", "Invalid scenario, locale, prompt variables or sample.")));
    }
}
