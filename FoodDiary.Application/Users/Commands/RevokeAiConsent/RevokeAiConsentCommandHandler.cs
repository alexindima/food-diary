using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.RevokeAiConsent;

public sealed class RevokeAiConsentCommandHandler(IUserContextService userContextService)
    : ICommandHandler<RevokeAiConsentCommand, Result> {
    public async Task<Result> Handle(RevokeAiConsentCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.Value;
        user.RevokeAiConsent();
        await userContextService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
