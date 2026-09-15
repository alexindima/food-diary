using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IAttentionSignalMetricsReadService {
    Task<IReadOnlyList<AttentionSignalMetricsReadModel>> GetAsync(
        IReadOnlyCollection<UserId> clientUserIds,
        DateTime dateFromUtc,
        DateTime dateToUtc,
        CancellationToken cancellationToken = default);
}
