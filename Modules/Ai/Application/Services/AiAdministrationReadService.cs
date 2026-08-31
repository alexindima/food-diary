using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;

namespace FoodDiary.Application.Ai.Services;

public sealed class AiAdministrationReadService(
    IAiUsageReadRepository usageRepository,
    IAiPromptTemplateReadModelRepository promptRepository) : IAiAdministrationReadService {
    public Task<AiUsageSummary> GetUsageSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) =>
        usageRepository.GetSummaryAsync(fromUtc, toUtc, cancellationToken);

    public Task<IReadOnlyList<AiPromptTemplateReadModel>> GetPromptTemplatesAsync(
        CancellationToken cancellationToken) =>
        promptRepository.GetAllReadModelsAsync(cancellationToken);
}
