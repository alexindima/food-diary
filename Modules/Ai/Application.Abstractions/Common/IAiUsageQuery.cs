using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiUsageQuery {
    Task<AiUsageSummary> GetSummaryForUserAsync(DateTime fromUtc, DateTime toUtc, UserId userId, CancellationToken cancellationToken);

    Task<AiUsageSummary> GetSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<AiUsageTotals> GetUserTotalsAsync(
        UserId userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
