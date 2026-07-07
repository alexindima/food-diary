using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiPromptTemplateReadModelRepository {
    Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);
}
