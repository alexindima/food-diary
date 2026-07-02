using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class AiUserContextService(IUserRepository userRepository) : IAiUserContextService {
    public async Task<Result<AiUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<AiUserContext>(accessError);
        }

        return Result.Success(new AiUserContext(
            user!.Id,
            user.Language,
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit));
    }
}
