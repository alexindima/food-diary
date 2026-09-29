using FoodDiary.Modules.Gamification.Contracts.Models;

namespace FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;

public interface IAchievementDefinitionReadModelRepository {
    Task<IReadOnlyList<AchievementDefinitionAdminModel>> GetForAdministrationAsync(
        int page = 1,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
