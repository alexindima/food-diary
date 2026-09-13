using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Ai;

public sealed class AiUsageRepository(DbSet<AiUsage> usages, IAiUsageQuery query, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IAiUsageRepository {
    public Task<AiUsageSummary> GetSummaryForUserAsync(DateTime fromUtc, DateTime toUtc, UserId userId, CancellationToken cancellationToken) =>
        query.GetSummaryForUserAsync(fromUtc, toUtc, userId, cancellationToken);

    public Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        query.GetSummaryAsync(fromUtc, toUtc, cancellationToken);

    public Task<AiUsageTotals> GetUserTotalsAsync(UserId userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        query.GetUserTotalsAsync(userId, fromUtc, toUtc, cancellationToken);

    public async Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        await usages.AddAsync(usage, cancellationToken).ConfigureAwait(false);
    }
}
