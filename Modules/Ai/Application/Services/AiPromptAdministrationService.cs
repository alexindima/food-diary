using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Services;

public sealed class AiPromptAdministrationService(IAiPromptTemplateWriteRepository repository)
    : IAiPromptAdministrationService {
    public async Task<Result<AiPromptTemplateReadModel>> UpsertAsync(
        string key,
        string locale,
        string promptText,
        bool isActive,
        CancellationToken cancellationToken) {
        AiPromptTemplate? existing = await repository.GetByKeyAsync(key, locale, cancellationToken).ConfigureAwait(false);
        if (existing is null) {
            var created = AiPromptTemplate.Create(key, locale, promptText, isActive);
            await repository.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return Result.Success(ToReadModel(created));
        }

        AiPromptTemplate? tracked = await repository
            .GetByIdAsync(existing.Id, asTracking: true, cancellationToken)
            .ConfigureAwait(false);
        if (tracked is null) {
            return Result.Failure<AiPromptTemplateReadModel>(AiErrors.PromptTemplateNotFound());
        }

        tracked.Update(promptText, isActive);
        await repository.UpdateAsync(tracked, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToReadModel(tracked));
    }
    private static AiPromptTemplateReadModel ToReadModel(AiPromptTemplate template) =>
        new(template.Id.Value, template.Key, template.Locale, template.PromptText, template.Version,
            template.IsActive, template.CreatedOnUtc, template.ModifiedOnUtc);
}
