using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Ai;

internal sealed class AiPromptTemplateRepository(FoodDiaryDbContext context) : IAiPromptTemplateRepository {
    public async Task<IReadOnlyList<AiPromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default) {
        return await context.Set<AiPromptTemplate>()
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) {
        return await context.Set<AiPromptTemplate>()
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .Select(t => new AiPromptTemplateReadModel(
                t.Id.Value,
                t.Key,
                t.Locale,
                t.PromptText,
                t.Version,
                t.IsActive,
                t.CreatedOnUtc,
                t.ModifiedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AiPromptTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default) {
        return await context.Set<AiPromptTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AiPromptTemplate?> GetByIdAsync(
        AiPromptTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<AiPromptTemplate> query = asTracking
            ? context.Set<AiPromptTemplate>().AsTracking()
            : context.Set<AiPromptTemplate>().AsNoTracking();

        return await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
        await context.Set<AiPromptTemplate>().AddAsync(template, cancellationToken).ConfigureAwait(false);
        return template;
    }

    public Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
        return Task.CompletedTask;
    }
}
