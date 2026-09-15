using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt;

public sealed class UpsertAiPromptCommandHandler(IAiPromptTemplateWriteRepository repository) : IRequestHandler<UpsertAiPromptCommand, Result<AiPromptTemplateReadModel>> {
    public async Task<Result<AiPromptTemplateReadModel>> Handle(UpsertAiPromptCommand request, CancellationToken cancellationToken) {
        string key = request.Key.Trim().ToLowerInvariant();
        if (!LanguageCode.TryParse(request.Locale, out LanguageCode language)) {
            return Result.Failure<AiPromptTemplateReadModel>(
                Errors.Validation.Invalid(nameof(request.Locale), "Locale must be one of the supported codes."));
        }
        string locale = language.Value;
        string promptText = request.PromptText;
        bool isActive = request.IsActive;
        AiPromptTemplate? existing = await repository.GetByKeyAsync(key, locale, cancellationToken).ConfigureAwait(false);
        if (existing is null) {
            var created = AiPromptTemplate.Create(key, locale, promptText, isActive);
            await repository.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return Result.Success(ToReadModel(created));
        }

        existing.Update(promptText, isActive);
        await repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToReadModel(existing));

    }
    private static AiPromptTemplateReadModel ToReadModel(AiPromptTemplate template) =>
        new(template.Id.Value, template.Key, template.Locale, template.PromptText, template.Version,
            template.IsActive, template.CreatedOnUtc, template.ModifiedOnUtc);

}
