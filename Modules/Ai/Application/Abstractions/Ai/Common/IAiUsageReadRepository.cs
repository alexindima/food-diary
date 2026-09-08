using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiUsageReadRepository {
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
