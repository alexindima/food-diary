using FoodDiary.Modules.Gamification.Contracts.Models;

namespace FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;

public interface IAchievementDefinitionReadModelRepository {
    Task<IReadOnlyList<AchievementDefinitionAdminModel>> GetForAdministrationAsync(CancellationToken cancellationToken = default);
}
