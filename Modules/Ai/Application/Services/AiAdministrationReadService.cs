using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Common;

namespace FoodDiary.Modules.Ai.Application.Services;

public sealed class AiAdministrationReadService(
    IAiUsageQuery usageRepository,
    IAiPromptTemplateReadModelRepository promptRepository) : IAiAdministrationReadService {
    public Task<IReadOnlyList<AiPromptRevisionReadModel>> GetPromptRevisionsAsync(string key, string locale, CancellationToken cancellationToken) =>
        promptRepository.GetRevisionsAsync(key, locale, cancellationToken);
    public Task<AiUsageSummary> GetUsageSummaryForUserAsync(DateTime fromUtc, DateTime toUtc, Guid userId, CancellationToken cancellationToken) =>
        usageRepository.GetSummaryForUserAsync(fromUtc, toUtc, new UserId(userId), cancellationToken);

    public Task<AiUsageSummary> GetUsageSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) =>
        usageRepository.GetSummaryAsync(fromUtc, toUtc, cancellationToken);

    public Task<IReadOnlyList<AiPromptTemplateReadModel>> GetPromptTemplatesAsync(
        CancellationToken cancellationToken) =>
        promptRepository.GetAllReadModelsAsync(cancellationToken);
}
