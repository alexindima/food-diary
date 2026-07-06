using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.ContentReports;

internal sealed class ContentReportRepository(FoodDiaryDbContext context) : IContentReportRepository {
    public async Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default) {
        await context.ContentReports.AddAsync(report, cancellationToken).ConfigureAwait(false);
        return report;
    }

    public async Task<ContentReport?> GetByIdAsync(
        ContentReportId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<ContentReport> query = asTracking ? context.ContentReports.AsTracking() : context.ContentReports.AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default) {
        context.ContentReports.Update(report);
        return Task.CompletedTask;
    }

    public async Task<bool> HasUserReportedAsync(
        UserId userId, ReportTargetType targetType, Guid targetId, CancellationToken cancellationToken = default) {
        return await context.ContentReports
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.TargetType == targetType && r.TargetId == targetId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<ContentReport> Items, int Total)> GetPagedAsync(
        ReportStatus? status, int page, int limit, CancellationToken cancellationToken = default) {
        IQueryable<ContentReport> query = context.ContentReports.AsNoTracking();

        if (status.HasValue) {
            query = query.Where(r => r.Status == status.Value);
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<ContentReport> items = await query
            .OrderByDescending(r => r.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        IQueryable<ContentReport> query = context.ContentReports.AsNoTracking();

        if (status.HasValue) {
            query = query.Where(r => r.Status == status.Value);
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<ContentReportAdminReadModel> items = await query
            .OrderByDescending(r => r.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => new ContentReportAdminReadModel(
                r.Id.Value,
                r.UserId.Value,
                r.TargetType.ToString(),
                r.TargetId,
                r.Reason,
                r.Status.ToString(),
                r.AdminNote,
                r.CreatedOnUtc,
                r.ReviewedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default) {
        return await context.ContentReports
            .AsNoTracking()
            .CountAsync(r => r.Status == status, cancellationToken).ConfigureAwait(false);
    }
}
