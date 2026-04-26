using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.ContentReports;

internal sealed class ContentReportRepository(FoodDiaryDbContext context) : IContentReportRepository {
    public async Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default) {
        await context.ContentReports.AddAsync(report, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<ContentReport?> GetByIdAsync(
        ContentReportId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        var query = asTracking ? context.ContentReports.AsTracking() : context.ContentReports.AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default) {
        context.ContentReports.Update(report);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasUserReportedAsync(
        UserId userId, ReportTargetType targetType, Guid targetId, CancellationToken cancellationToken = default) {
        return await context.ContentReports
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.TargetType == targetType && r.TargetId == targetId, cancellationToken);
    }

    public async Task<(IReadOnlyList<ContentReport> Items, int Total)> GetPagedAsync(
        ReportStatus? status, int page, int limit, CancellationToken cancellationToken = default) {
        var query = context.ContentReports.AsNoTracking();

        if (status.HasValue) {
            query = query.Where(r => r.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default) {
        return await context.ContentReports
            .AsNoTracking()
            .CountAsync(r => r.Status == status, cancellationToken);
    }
}
