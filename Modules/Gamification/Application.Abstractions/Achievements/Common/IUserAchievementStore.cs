using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;

public interface IUserAchievementStore {
    Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAchievement>> GrantMissingAsync(
        UserId userId,
        IReadOnlyCollection<AchievementGrantModel> grants,
        CancellationToken cancellationToken = default);
}
