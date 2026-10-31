using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Ai.Infrastructure.Persistence;

internal sealed class AiPromptTemplateRepository(DbSet<AiPromptTemplate> templates, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IAiPromptTemplateReadModelRepository, IAiPromptTemplateWriteRepository {
    public async Task<IReadOnlyList<AiPromptRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await templates.AsNoTracking().Where(template => template.Key == key && template.Locale == locale)
            .SelectMany(template => template.Revisions).OrderByDescending(revision => revision.ArchivedOnUtc).ThenByDescending(revision => revision.Id).Take(50)
            .Select(revision => new AiPromptRevisionReadModel(revision.Id, revision.PromptText, revision.Version,
                revision.IsActive, revision.SavedOnUtc, revision.ArchivedOnUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await templates
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
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiPromptTemplateReadModel>> GetPageReadModelsAsync(
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await templates.AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(t => new AiPromptTemplateReadModel(
                t.Id.Value, t.Key, t.Locale, t.PromptText, t.Version, t.IsActive, t.CreatedOnUtc, t.ModifiedOnUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AiPromptTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await templates
            .AsTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken);
    }

    public async Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await templates.AddAsync(template, cancellationToken);
        return template;
    }

    public async Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }

    }
}
