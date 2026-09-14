using FoodDiary.Modules.Ai.Domain.Entities;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiPromptTemplateWriteRepository {
    Task<AiPromptTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default);

    Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken = default);
}
