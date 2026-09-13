using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

internal sealed class RecommendationTemplateRepository(DbSet<RecommendationTemplate> records) : IRecommendationTemplateRepository {
    public async Task<RecommendationTemplate> AddAsync(
        RecommendationTemplate template,
        CancellationToken cancellationToken = default) {
        await records.AddAsync(template, cancellationToken).ConfigureAwait(false);
        return template;
    }

    public Task<RecommendationTemplate?> GetByIdAsync(
        RecommendationTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<RecommendationTemplate> query = records;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(template => template.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RecommendationTemplateReadModel>> SearchAsync(
        UserId dietologistUserId,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default) {
        IQueryable<RecommendationTemplate> query = records
            .AsNoTracking()
            .Where(template =>
                template.DietologistUserId == dietologistUserId &&
                (includeArchived || !template.IsArchived));
        if (string.IsNullOrWhiteSpace(search)) {
            return await query
                .OrderBy(template => template.Name)
                .Select(template => new RecommendationTemplateReadModel(
                    template.Id.Value,
                    template.Name,
                    template.Text,
                    template.IsArchived,
                    template.CreatedOnUtc,
                    template.ModifiedOnUtc))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        string pattern = $"%{search.Trim()}%";
        query = query.Where(template =>
            EF.Functions.ILike(template.Name, pattern) ||
            EF.Functions.ILike(template.Text, pattern));

        return await query
            .OrderBy(template => template.Name)
            .Select(template => new RecommendationTemplateReadModel(
                template.Id.Value,
                template.Name,
                template.Text,
                template.IsArchived,
                template.CreatedOnUtc,
                template.ModifiedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
