using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.Entities;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiPromptTemplateReadRepository {
    Task<IReadOnlyList<AiPromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AiPromptTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);

    Task<AiPromptTemplate?> GetByIdAsync(
        AiPromptTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);
}
