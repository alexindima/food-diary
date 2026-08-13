using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Ai.Common;

public interface IAiAdministrationReadService {
    Task<AiUsageSummary> GetUsageSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AiPromptTemplateReadModel>> GetPromptTemplatesAsync(
        CancellationToken cancellationToken);
}
