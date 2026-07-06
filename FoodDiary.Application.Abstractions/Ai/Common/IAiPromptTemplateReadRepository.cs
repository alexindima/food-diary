using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Ai.Common;

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