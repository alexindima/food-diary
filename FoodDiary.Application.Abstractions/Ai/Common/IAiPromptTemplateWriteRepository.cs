using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiPromptTemplateWriteRepository : IAiPromptTemplateReadRepository {
    Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken = default);
}
