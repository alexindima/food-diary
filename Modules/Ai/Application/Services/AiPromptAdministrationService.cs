using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Services;

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

        existing.Update(promptText, isActive);
        await repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToReadModel(existing));
    }
    private static AiPromptTemplateReadModel ToReadModel(AiPromptTemplate template) =>
        new(template.Id.Value, template.Key, template.Locale, template.PromptText, template.Version,
            template.IsActive, template.CreatedOnUtc, template.ModifiedOnUtc);
}
