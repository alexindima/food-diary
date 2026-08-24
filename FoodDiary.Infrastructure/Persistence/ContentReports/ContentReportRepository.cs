using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.ContentReports;

internal sealed class ContentReportRepository(FoodDiaryDbContext context)
    : IContentReportReadModelRepository, IContentReportWriteRepository, IContentReportTargetReadService {

    public Task<bool> IsReportableAsync(
        UserId reporterUserId,
        ReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default) =>
        targetType switch {
            ReportTargetType.Recipe => context.Recipes.AsNoTracking().AnyAsync(
                recipe => recipe.Id == new RecipeId(targetId)
                    && (recipe.Visibility == Visibility.Public || recipe.UserId == reporterUserId),
                cancellationToken),
            ReportTargetType.Comment => context.RecipeComments.AsNoTracking().AnyAsync(
                comment => comment.Id == new RecipeCommentId(targetId)
                    && (comment.Recipe.Visibility == Visibility.Public || comment.Recipe.UserId == reporterUserId),
                cancellationToken),
            _ => Task.FromResult(false),
        };
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

    public async Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<ContentReport> query = context.ContentReports.AsNoTracking();

        if (status.HasValue) {
            query = query.Where(r => r.Status == status.Value);
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<ContentReportAdminReadModel> items = await query
            .OrderByDescending(r => r.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
