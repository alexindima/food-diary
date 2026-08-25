using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class AiUserContextService(IUserAiProfileReadService userProfileReadService) : IAiUserContextService {
    public async Task<Result<AiUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserAiProfileModel> profileResult = await userProfileReadService
            .GetAiProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<AiUserContext>(profileResult.Error);
        }

        UserAiProfileModel profile = profileResult.Value;
        return Result.Success(new AiUserContext(
            profile.UserId,
            profile.Language,
            profile.InputTokenLimit,
            profile.OutputTokenLimit,
            profile.HasAcceptedAiConsent));
    }
}
