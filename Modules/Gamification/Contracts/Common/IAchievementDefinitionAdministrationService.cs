using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Gamification.Common;

public interface IAchievementDefinitionAdministrationService {
    Task<IReadOnlyList<AchievementDefinitionAdminModel>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<AchievementDefinitionAdminModel>> CreateAsync(AchievementDefinitionCreateInput input, CancellationToken cancellationToken);
    Task<Result<AchievementDefinitionAdminModel>> UpdateAsync(Guid id, AchievementDefinitionUpdateInput input, CancellationToken cancellationToken);
}
