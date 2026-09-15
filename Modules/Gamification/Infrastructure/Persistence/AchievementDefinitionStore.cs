using FoodDiary.Modules.Gamification.Domain.ValueObjects.Ids;
using System.Data.Common;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Gamification.Infrastructure.Persistence;

public sealed class AchievementDefinitionStore(DbContext context, DbSet<AchievementDefinition> definitions, DbSet<UserAchievement> achievements, Func<DbTransaction?>? currentTransaction = null) : IAchievementDefinitionStore, IAchievementDefinitionReadModelRepository {
    public async Task<IReadOnlyList<AchievementDefinitionAdminModel>> GetForAdministrationAsync(CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<string, int> counts = await GetAwardCountsAsync(cancellationToken).ConfigureAwait(false);
        List<AchievementDefinitionAdminModel> models = await definitions.AsNoTracking()
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Key)
            .Select(item => new AchievementDefinitionAdminModel(
                item.Id.Value, item.Key, item.Category, item.Metric.ToString(), item.Threshold,
                item.TitleRu, item.TitleEn, item.DescriptionRu, item.DescriptionEn, item.Icon,
                item.SortOrder, item.IsActive, item.Version, 0))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. models.Select(item => item with { AwardedUsers = counts.GetValueOrDefault(item.Key) })];
    }

    public async Task<IReadOnlyDictionary<string, int>> GetAwardCountsAsync(CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await achievements.AsNoTracking().GroupBy(item => item.AchievementKey)
            .Select(group => new { group.Key, Count = group.Select(item => item.UserId).Distinct().Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, StringComparer.Ordinal, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AchievementDefinition>> GetAllAsync(CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await definitions.AsNoTracking().OrderBy(item => item.SortOrder).ThenBy(item => item.Key)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AchievementDefinition>> GetActiveAsync(CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await definitions.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Key).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AchievementDefinition?> GetByIdTrackingAsync(
        AchievementDefinitionId id,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await definitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryAddAsync(
        AchievementDefinition definition,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
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
        definitions.Update(definition);
        return Task.CompletedTask;
    }
    private async Task SynchronizeTransactionAsync(CancellationToken cancellationToken) {
        if (currentTransaction is not null && context.Database.IsRelational()) {
            await context.Database.UseTransactionAsync(currentTransaction(), cancellationToken).ConfigureAwait(false);
        }
    }
}
