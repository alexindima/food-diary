using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Lessons.Application.Abstractions.Common;

public interface ILessonProgressTransactionRunner {
    Task<T> ExecuteSerializedAsync<T>(UserId userId, NutritionLessonId lessonId,
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
