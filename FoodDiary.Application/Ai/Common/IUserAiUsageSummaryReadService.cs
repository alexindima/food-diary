using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Common;

public interface IUserAiUsageSummaryReadService {
    Task<Result<UserAiUsageModel>> GetAsync(UserId userId, CancellationToken cancellationToken);
}
