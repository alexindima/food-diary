using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiUsageReadRepository {
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
