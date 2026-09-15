using FoodDiary.Modules.Gamification.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;

namespace FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;

public interface IAchievementDefinitionStore {
    Task<IReadOnlyDictionary<string, int>> GetAwardCountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AchievementDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AchievementDefinition>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<AchievementDefinition?> GetByIdTrackingAsync(AchievementDefinitionId id, CancellationToken cancellationToken = default);
    Task<bool> TryAddAsync(AchievementDefinition definition, CancellationToken cancellationToken = default);
    Task UpdateAsync(AchievementDefinition definition, CancellationToken cancellationToken = default);
}
