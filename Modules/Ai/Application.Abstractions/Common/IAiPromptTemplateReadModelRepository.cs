using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiPromptTemplateReadModelRepository {
    Task<IReadOnlyList<AiPromptRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);
}
