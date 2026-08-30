using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IAttentionSignalMetricsReadService {
    Task<IReadOnlyList<AttentionSignalMetricsReadModel>> GetAsync(
        IReadOnlyCollection<UserId> clientUserIds,
        DateTime dateFromUtc,
        DateTime dateToUtc,
        CancellationToken cancellationToken = default);
}
