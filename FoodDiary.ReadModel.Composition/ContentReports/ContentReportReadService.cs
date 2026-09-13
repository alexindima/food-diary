using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.ContentReports;

public sealed class ContentReportReadService(FoodDiaryDbContext context)
    : IContentReportReadModelRepository, IContentReportTargetReadService {

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
                    && context.Recipes.AsNoTracking().Any(recipe => recipe.Id == comment.RecipeId && (recipe.Visibility == Visibility.Public || recipe.UserId == reporterUserId)),
                cancellationToken),
            _ => Task.FromResult(false),
        };

    public async Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default, ContentReportAdminFilter? filter = null) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<ContentReport> query = context.ContentReports.AsNoTracking();

        if (status.HasValue) {
            query = query.AsNoTracking().Where(r => r.Status == status.Value);
        }

        if (filter?.FromUtc is { } from) { query = query.AsNoTracking().Where(report => report.CreatedOnUtc >= from); }
        if (filter?.ToUtc is { } to) { query = query.AsNoTracking().Where(report => report.CreatedOnUtc < to); }
        if (filter?.ReporterId is { } reporter) { query = query.AsNoTracking().Where(report => report.UserId == new UserId(reporter)); }
        if (filter?.TargetId is { } target) { query = query.AsNoTracking().Where(report => report.TargetId == target); }
        if (Enum.TryParse(filter?.TargetType, out ReportTargetType type)) { query = query.AsNoTracking().Where(report => report.TargetType == type); }

        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);

        List<ContentReportAdminReadModel> items = await query.AsNoTracking()
            .OrderByDescending(r => r.CreatedOnUtc).ThenByDescending(r => r.Id)
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
                r.ReviewedAtUtc,
                r.ReviewedByUserId.HasValue ? r.ReviewedByUserId.Value.Value : null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        RecipeId[] recipeIds = [.. items.Where(item => string.Equals(item.TargetType, nameof(ReportTargetType.Recipe), StringComparison.Ordinal)).Select(item => new RecipeId(item.TargetId))];
        RecipeCommentId[] commentIds = [.. items.Where(item => string.Equals(item.TargetType, nameof(ReportTargetType.Comment), StringComparison.Ordinal)).Select(item => new RecipeCommentId(item.TargetId))];
        Dictionary<Guid, string> titles = await context.Recipes.AsNoTracking().Where(recipe => Enumerable.Contains(recipeIds, recipe.Id))
            .Select(recipe => new { Id = recipe.Id.Value, recipe.Name })
            .ToDictionaryAsync(recipe => recipe.Id, recipe => recipe.Name, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> excerpts = await context.RecipeComments.AsNoTracking().Where(comment => Enumerable.Contains(commentIds, comment.Id))
            .Select(comment => new { Id = comment.Id.Value, Text = comment.Text.Substring(0, Math.Min(comment.Text.Length, 1000)) })
            .ToDictionaryAsync(comment => comment.Id, comment => comment.Text, cancellationToken).ConfigureAwait(false);
        return (items.ConvertAll(item => item with { TargetTitle = titles.GetValueOrDefault(item.TargetId), TargetExcerpt = excerpts.GetValueOrDefault(item.TargetId) }), total);
    }

    public async Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default) {
        return await context.ContentReports
            .AsNoTracking()
            .CountAsync(r => r.Status == status, cancellationToken).ConfigureAwait(false);
    }
}
