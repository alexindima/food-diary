using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiPromptTemplateReadModelRepository {
    Task<IReadOnlyList<AiPromptRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);
}
