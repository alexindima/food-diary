using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;

namespace FoodDiary.Modules.WeeklyGoals.Application.Common;

public sealed class WeeklyGoalProgressReader(ISender mealActivityReadService) {
    public async Task<int> GetProgressDaysAsync(WeeklyGoal goal, CancellationToken cancellationToken) {
        DateTime weekEndUtc = goal.WeekStartUtc.AddDays(6);
        IReadOnlyList<DateTime> dates = await mealActivityReadService.Send(new ReadDistinctMealDatesQuery(
            goal.UserId,
            goal.WeekStartUtc,
            weekEndUtc),
            cancellationToken).ConfigureAwait(false);
        return dates.Count;
    }
}
