using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

public sealed class AchievementDefinitionStore(FoodDiaryDbContext context) : IAchievementDefinitionStore {
    public async Task<IReadOnlyList<AchievementDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.AchievementDefinitions.AsNoTracking().OrderBy(item => item.SortOrder).ThenBy(item => item.Key)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<AchievementDefinition>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await context.AchievementDefinitions.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Key).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<AchievementDefinition?> GetByIdTrackingAsync(
        AchievementDefinitionId id,
        CancellationToken cancellationToken = default) =>
        context.AchievementDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<bool> TryAddAsync(
        AchievementDefinition definition,
        CancellationToken cancellationToken = default) {
        int inserted = await context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO "AchievementDefinitions"
                ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc", "ModifiedOnUtc")
            VALUES
                ({{definition.Id.Value}}, {{definition.Key}}, {{definition.Category}}, {{definition.Metric.ToString()}}, {{definition.Threshold}}, {{definition.TitleRu}}, {{definition.TitleEn}}, {{definition.DescriptionRu}}, {{definition.DescriptionEn}}, {{definition.Icon}}, {{definition.SortOrder}}, {{definition.IsActive}}, {{definition.Version}}, {{definition.CreatedOnUtc}}, {{definition.ModifiedOnUtc}})
            ON CONFLICT ("Key") DO NOTHING
            """, cancellationToken).ConfigureAwait(false);
        return inserted == 1;
    }

    public Task UpdateAsync(AchievementDefinition definition, CancellationToken cancellationToken = default) {
        context.AchievementDefinitions.Update(definition);
        return Task.CompletedTask;
    }
}
