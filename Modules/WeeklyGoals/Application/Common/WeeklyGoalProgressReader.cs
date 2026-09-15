using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Application.Common;

public sealed class WeeklyGoalProgressReader(ISender mealActivityReadService) {
    public Task<int> GetProgressDaysAsync(WeeklyGoal goal, CancellationToken cancellationToken) =>
        GetProgressDaysAsync(goal.UserId, goal.WeekStartUtc, cancellationToken);

    public async Task<int> GetProgressDaysAsync(UserId userId, DateTime weekStartUtc, CancellationToken cancellationToken) {
        DateTime weekEndUtc = weekStartUtc.AddDays(6);
        IReadOnlyList<DateTime> dates = await mealActivityReadService.Send(new ReadDistinctMealDatesQuery(
            userId,
            weekStartUtc,
            weekEndUtc),
            cancellationToken).ConfigureAwait(false);
        return dates.Count;
    }
}
