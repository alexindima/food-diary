using FoodDiary.Application.Gamification.Models;

namespace FoodDiary.Application.Abstractions.Achievements.Common;

public interface IAchievementDefinitionReadModelRepository {
    Task<IReadOnlyList<AchievementDefinitionAdminModel>> GetForAdministrationAsync(CancellationToken cancellationToken = default);
}
