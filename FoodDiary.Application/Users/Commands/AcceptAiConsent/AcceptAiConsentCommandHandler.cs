using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.AcceptAiConsent;

public sealed class AcceptAiConsentCommandHandler(IUserContextService userContextService)
    : ICommandHandler<AcceptAiConsentCommand, Result> {
    public async Task<Result> Handle(AcceptAiConsentCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.Value;
        user.AcceptAiConsent();
        await userContextService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
