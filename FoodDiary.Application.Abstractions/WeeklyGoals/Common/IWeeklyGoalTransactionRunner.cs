using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WeeklyGoals.Common;

public interface IWeeklyGoalTransactionRunner {
    Task<T> ExecuteSerializedAsync<T>(
        UserId userId,
        DateTime weekStartUtc,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
