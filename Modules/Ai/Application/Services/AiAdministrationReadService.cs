using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class AiAdministrationReadService(
    IAiUsageReadRepository usageRepository,
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
