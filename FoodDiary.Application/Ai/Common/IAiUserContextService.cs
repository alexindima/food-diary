using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Common;

public interface IAiUserContextService {
    Task<Result<AiUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
