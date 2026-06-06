using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Ai;

public sealed class AiUsageRepository(FoodDiaryDbContext context) : IAiUsageRepository {
    public async Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) {
        await context.AiUsages.AddAsync(usage, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AiUsageSummary> GetSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) {
        IQueryable<AiUsage> query = CreateSummaryQuery(fromUtc, toUtc);

        AiUsageTotalsRow? totals = await GetSummaryTotalsAsync(query, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AiUsageDailySummary> daily = await GetDailySummaryAsync(query, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AiUsageBreakdown> byOperation = await GetBreakdownByOperationAsync(query, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AiUsageBreakdown> byModel = await GetBreakdownByModelAsync(query, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AiUsageUserSummary> byUser = await GetBreakdownByUserAsync(query, cancellationToken).ConfigureAwait(false);

        return new AiUsageSummary(
            totals?.TotalTokens ?? 0,
            totals?.InputTokens ?? 0,
            totals?.OutputTokens ?? 0,
            daily,
            byOperation,
            byModel,
            byUser);
    }

    private IQueryable<AiUsage> CreateSummaryQuery(DateTime fromUtc, DateTime toUtc) {
        return context.AiUsages
            .AsNoTracking()
            .Where(x => x.CreatedOnUtc >= fromUtc && x.CreatedOnUtc < toUtc);
    }

    private static async Task<AiUsageTotalsRow?> GetSummaryTotalsAsync(IQueryable<AiUsage> query, CancellationToken cancellationToken) {
        return await query
            .GroupBy(_ => 1)
            .Select(group => new AiUsageTotalsRow(
                group.Sum(x => x.TotalTokens),
                group.Sum(x => x.InputTokens),
                group.Sum(x => x.OutputTokens)))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<AiUsageDailySummary>> GetDailySummaryAsync(
        IQueryable<AiUsage> query,
        CancellationToken cancellationToken) {
        var byDay = await query
            .GroupBy(x => x.CreatedOnUtc.Date)
            .Select(group => new {
                Date = group.Key,
                Total = group.Sum(x => x.TotalTokens),
                Input = group.Sum(x => x.InputTokens),
                Output = group.Sum(x => x.OutputTokens),
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return byDay
            .ConvertAll(x => new AiUsageDailySummary(
                DateOnly.FromDateTime(x.Date),
                x.Total,
                x.Input,
                x.Output));
    }

    private static async Task<IReadOnlyList<AiUsageBreakdown>> GetBreakdownByOperationAsync(
        IQueryable<AiUsage> query,
        CancellationToken cancellationToken) {
        var byOperationRaw = await query
            .GroupBy(x => x.Operation)
            .Select(group => new {
                group.Key,
                Total = group.Sum(x => x.TotalTokens),
                Input = group.Sum(x => x.InputTokens),
                Output = group.Sum(x => x.OutputTokens),
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return byOperationRaw
            .ConvertAll(x => new AiUsageBreakdown(x.Key, x.Total, x.Input, x.Output));
    }

    private static async Task<IReadOnlyList<AiUsageBreakdown>> GetBreakdownByModelAsync(
        IQueryable<AiUsage> query,
        CancellationToken cancellationToken) {
        var byModelRaw = await query
            .GroupBy(x => x.Model)
            .Select(group => new {
                group.Key,
                Total = group.Sum(x => x.TotalTokens),
                Input = group.Sum(x => x.InputTokens),
                Output = group.Sum(x => x.OutputTokens),
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return byModelRaw
            .ConvertAll(x => new AiUsageBreakdown(x.Key, x.Total, x.Input, x.Output));
    }

    private async Task<IReadOnlyList<AiUsageUserSummary>> GetBreakdownByUserAsync(
        IQueryable<AiUsage> query,
        CancellationToken cancellationToken) {
        var byUserRaw = await query
            .Join(
                context.Users.AsNoTracking(),
                usage => usage.UserId,
                user => user.Id,
                (usage, user) => new { usage, user.Id, user.Email })
            .GroupBy(x => new { x.Id, x.Email })
            .Select(group => new {
                group.Key.Id,
                group.Key.Email,
                Total = group.Sum(x => x.usage.TotalTokens),
                Input = group.Sum(x => x.usage.InputTokens),
                Output = group.Sum(x => x.usage.OutputTokens),
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return byUserRaw
            .ConvertAll(x => new AiUsageUserSummary(new UserId(x.Id), x.Email, x.Total, x.Input, x.Output));
    }

    public async Task<AiUsageTotals> GetUserTotalsAsync(
        UserId userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) {
        AiUsageTotals? totals = await context.AiUsages
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.CreatedOnUtc >= fromUtc && x.CreatedOnUtc < toUtc)
            .GroupBy(_ => 1)
            .Select(group => new AiUsageTotals(
                group.Sum(x => (long)x.InputTokens),
                group.Sum(x => (long)x.OutputTokens)))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return totals ?? new AiUsageTotals(0, 0);
    }

    private sealed record AiUsageTotalsRow(int TotalTokens, int InputTokens, int OutputTokens);
}
