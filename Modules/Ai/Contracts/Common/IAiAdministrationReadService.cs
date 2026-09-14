using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Contracts.Common;

public interface IAiAdministrationReadService {
    Task<IReadOnlyList<AiPromptRevisionReadModel>> GetPromptRevisionsAsync(string key, string locale, CancellationToken cancellationToken);
    Task<AiUsageSummary> GetUsageSummaryForUserAsync(DateTime fromUtc, DateTime toUtc, Guid userId, CancellationToken cancellationToken);

    Task<AiUsageSummary> GetUsageSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AiPromptTemplateReadModel>> GetPromptTemplatesAsync(
        CancellationToken cancellationToken);
}
