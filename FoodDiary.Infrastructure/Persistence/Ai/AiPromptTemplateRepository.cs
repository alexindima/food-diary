using FoodDiary.Application.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Ai;

internal sealed class AiPromptTemplateRepository(FoodDiaryDbContext context) : IAiPromptTemplateRepository {
    public async Task<IReadOnlyList<AiPromptTemplate>> GetAllAsync(CancellationToken cancellationToken) {
        return await context.Set<AiPromptTemplate>()
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiPromptTemplate?> GetByKeyAsync(
        string key,
        string locale,
        CancellationToken cancellationToken) {
        return await context.Set<AiPromptTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken);
    }

    public async Task<AiPromptTemplate?> GetByIdAsync(
        AiPromptTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = asTracking
            ? context.Set<AiPromptTemplate>().AsTracking()
            : context.Set<AiPromptTemplate>().AsNoTracking();

        return await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken) {
        context.Set<AiPromptTemplate>().Add(template);
        await context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken) {
        await context.SaveChangesAsync(cancellationToken);
    }
}
