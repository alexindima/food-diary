namespace FoodDiary.Application.Abstractions.Achievements.Common;

public interface IAchievementEvaluationOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
