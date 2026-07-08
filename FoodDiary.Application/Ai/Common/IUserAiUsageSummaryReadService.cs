using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Common;

public interface IUserAiUsageSummaryReadService {
    Task<Result<UserAiUsageModel>> GetAsync(UserId userId, CancellationToken cancellationToken);
}
