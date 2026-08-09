using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Achievements.Common;

public interface IAchievementReconciliationHandler {
    Task ReconcileAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken = default);
}
