using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class AiUsageRepository(FoodDiaryDbContext context) : IAiUsageRepository
{
    public async Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default)
    {
        await context.AiUsages.AddAsync(usage, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiUsageSummary> GetSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = context.AiUsages
            .AsNoTracking()
            .Where(x => x.CreatedOnUtc >= fromUtc && x.CreatedOnUtc < toUtc);

        var totalTokens = await query.SumAsync(x => x.TotalTokens, cancellationToken);
        var inputTokens = await query.SumAsync(x => x.InputTokens, cancellationToken);
        var outputTokens = await query.SumAsync(x => x.OutputTokens, cancellationToken);

        var byDay = await query
            .GroupBy(x => x.CreatedOnUtc.Date)
            .Select(group => new
            {
                Date = group.Key,
                Total = group.Sum(x => x.TotalTokens),
                Input = group.Sum(x => x.InputTokens),
                Output = group.Sum(x => x.OutputTokens)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var byOperationRaw = await query
            .GroupBy(x => x.Operation)
            .Select(group => new
            {
                Key = group.Key,
                Total = group.Sum(x => x.TotalTokens),
                Input = group.Sum(x => x.InputTokens),
                Output = group.Sum(x => x.OutputTokens)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        var byModelRaw = await query
            .GroupBy(x => x.Model)
            .Select(group => new
            {
                Key = group.Key,
                Total = group.Sum(x => x.TotalTokens),
                Input = group.Sum(x => x.InputTokens),
                Output = group.Sum(x => x.OutputTokens)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        var byUserRaw = await query
            .Join(
                context.Users.AsNoTracking(),
                usage => usage.UserId,
                user => user.Id,
                (usage, user) => new { usage, user.Id, user.Email })
            .GroupBy(x => new { x.Id, x.Email })
            .Select(group => new
            {
                group.Key.Id,
                group.Key.Email,
                Total = group.Sum(x => x.usage.TotalTokens),
                Input = group.Sum(x => x.usage.InputTokens),
                Output = group.Sum(x => x.usage.OutputTokens)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        var daily = byDay
            .Select(x => new AiUsageDailySummary(
                DateOnly.FromDateTime(x.Date),
                x.Total,
                x.Input,
                x.Output))
            .ToList();

        var byOperation = byOperationRaw
            .Select(x => new AiUsageBreakdown(x.Key, x.Total, x.Input, x.Output))
            .ToList();

        var byModel = byModelRaw
            .Select(x => new AiUsageBreakdown(x.Key, x.Total, x.Input, x.Output))
            .ToList();

        var byUser = byUserRaw
            .Select(x => new AiUsageUserSummary(new UserId(x.Id), x.Email, x.Total, x.Input, x.Output))
            .ToList();

        return new AiUsageSummary(
            totalTokens,
            inputTokens,
            outputTokens,
            daily,
            byOperation,
            byModel,
            byUser);
    }

    public async Task<AiUsageTotals> GetUserTotalsAsync(
        UserId userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var totals = await context.AiUsages
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.CreatedOnUtc >= fromUtc && x.CreatedOnUtc < toUtc)
            .GroupBy(_ => 1)
            .Select(group => new AiUsageTotals(
                group.Sum(x => (long)x.InputTokens),
                group.Sum(x => (long)x.OutputTokens)))
            .FirstOrDefaultAsync(cancellationToken);

        return totals ?? new AiUsageTotals(0, 0);
    }
}
