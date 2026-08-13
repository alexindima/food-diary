using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.Entities.WeeklyGoals;

namespace FoodDiary.Application.WeeklyGoals.Common;

public sealed class WeeklyGoalProgressReader(IMealActivityReadService mealActivityReadService) {
    public async Task<int> GetProgressDaysAsync(WeeklyGoal goal, CancellationToken cancellationToken) {
        DateTime weekEndUtc = goal.WeekStartUtc.AddDays(6);
        IReadOnlyList<DateTime> dates = await mealActivityReadService.GetDistinctMealDatesAsync(
            goal.UserId,
            goal.WeekStartUtc,
            weekEndUtc,
            cancellationToken).ConfigureAwait(false);
        return dates.Count;
    }
}
