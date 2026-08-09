using FoodDiary.Application.Abstractions.Achievements.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Achievements.Common;

public interface IUserAchievementStore {
    Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAchievement>> GrantMissingAsync(
        UserId userId,
        IReadOnlyCollection<AchievementGrantModel> grants,
        CancellationToken cancellationToken = default);
}
