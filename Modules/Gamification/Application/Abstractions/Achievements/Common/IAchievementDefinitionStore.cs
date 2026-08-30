using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Achievements.Common;

public interface IAchievementDefinitionStore {
    Task<IReadOnlyList<AchievementDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AchievementDefinition>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<AchievementDefinition?> GetByIdTrackingAsync(AchievementDefinitionId id, CancellationToken cancellationToken = default);
    Task<bool> TryAddAsync(AchievementDefinition definition, CancellationToken cancellationToken = default);
    Task UpdateAsync(AchievementDefinition definition, CancellationToken cancellationToken = default);
}
