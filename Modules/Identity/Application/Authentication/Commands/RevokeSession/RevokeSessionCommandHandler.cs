using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler(
    IRefreshTokenSessionWriteRepository repository,
    TimeProvider timeProvider) : ICommandHandler<RevokeSessionCommand, Result> {
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken) {
        await repository.RevokeOtherByIdAsync(
            command.SessionId,
            (UserId)command.UserId,
            command.CurrentSessionId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
