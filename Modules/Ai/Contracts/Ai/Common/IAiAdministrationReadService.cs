using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Abstractions.Ai.Common;

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
