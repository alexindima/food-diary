using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Application.Common;

public sealed class WeeklyGoalProgressReader(ISender mealActivityReadService) {
    public Task<int> GetProgressDaysAsync(WeeklyGoal goal, CancellationToken cancellationToken, TimeZoneInfo? timeZone = null) =>
        GetProgressDaysAsync(goal.UserId, goal.WeekStartUtc, cancellationToken, timeZone);

    public async Task<int> GetProgressDaysAsync(UserId userId, DateTime weekStartUtc, CancellationToken cancellationToken, TimeZoneInfo? timeZone = null) {
        var week = DateOnly.FromDateTime(weekStartUtc);
        DateTime from = timeZone is null ? weekStartUtc : LocalCalendar.StartOfDayUtc(week, timeZone);
        DateTime weekEndUtc = timeZone is null ? weekStartUtc.AddDays(6) : LocalCalendar.StartOfDayUtc(week.AddDays(7), timeZone).AddTicks(-1);
        IReadOnlyList<DateTime> dates = await mealActivityReadService.Send(new ReadDistinctMealDatesQuery(
            userId,
            from,
            weekEndUtc, timeZone),
            cancellationToken).ConfigureAwait(false);
        return dates.Count;
    }
}
