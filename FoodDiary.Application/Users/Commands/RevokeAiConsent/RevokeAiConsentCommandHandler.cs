using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Commands.RevokeAiConsent;

public class RevokeAiConsentCommandHandler(IUserRepository userRepository)
    : ICommandHandler<RevokeAiConsentCommand, Result> {
    public async Task<Result> Handle(RevokeAiConsentCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        user!.RevokeAiConsent();
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}
