using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.ContentReports.Infrastructure.Persistence;

internal sealed class ContentReportRepository(DbSet<ContentReport> reports) : IContentReportWriteRepository {
    public async Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default) {
        await reports.AddAsync(report, cancellationToken).ConfigureAwait(false);
        return report;
    }

    public async Task<ContentReport?> GetByIdAsync(
        ContentReportId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<ContentReport> query = asTracking ? reports.AsTracking() : reports.AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default) {
        reports.Update(report);
        return Task.CompletedTask;
    }

    public async Task<bool> HasUserReportedAsync(
        UserId userId, ReportTargetType targetType, Guid targetId, CancellationToken cancellationToken = default) {
        return await reports
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.TargetType == targetType && r.TargetId == targetId, cancellationToken).ConfigureAwait(false);
    }

}
