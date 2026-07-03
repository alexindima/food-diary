using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class AiUserContextService(IUserContextService userContextService) : IAiUserContextService {
    public async Task<Result<AiUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<AiUserContext>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new AiUserContext(
            user.Id,
            user.Language,
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit));
    }
}
