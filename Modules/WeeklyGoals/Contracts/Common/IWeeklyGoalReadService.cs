using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyGoals.Common;

public interface IWeeklyGoalReadService {
    Task<WeeklyGoalModel?> GetAsync(UserId userId, DateTime weekStartUtc, CancellationToken cancellationToken);
}
