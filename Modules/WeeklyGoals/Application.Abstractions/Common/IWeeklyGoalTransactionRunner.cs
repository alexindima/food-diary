using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;

public interface IWeeklyGoalTransactionRunner {
    Task<T> ExecuteSerializedAsync<T>(
        UserId userId,
        DateTime weekStartUtc,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
