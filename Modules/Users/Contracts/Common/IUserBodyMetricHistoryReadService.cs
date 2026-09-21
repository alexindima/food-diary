using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserBodyMetricHistoryReadService {
    Task<Result<WeightHistoryProfileModel>> GetWeightHistoryProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<WaistHistoryProfileModel>> GetWaistHistoryProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<WeightGoalHistoryModel>>> ReadWeightGoalsAsync(UserId userId, DateTime snapshotUtc, int offset, int limit, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<WaistGoalHistoryModel>>> ReadWaistGoalsAsync(UserId userId, DateTime snapshotUtc, int offset, int limit, CancellationToken cancellationToken);
}
