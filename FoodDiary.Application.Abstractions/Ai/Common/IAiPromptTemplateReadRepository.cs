using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiPromptTemplateReadRepository {
    Task<IReadOnlyList<AiPromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) {
        IReadOnlyList<AiPromptTemplate> templates = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. templates.Select(ToReadModel)];
    }

    Task<AiPromptTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);

    Task<AiPromptTemplate?> GetByIdAsync(
        AiPromptTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    private static AiPromptTemplateReadModel ToReadModel(AiPromptTemplate template) =>
        new(
            template.Id.Value,
            template.Key,
            template.Locale,
            template.PromptText,
            template.Version,
            template.IsActive,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);
}
