using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.RevokeOtherSessions;

public sealed class RevokeOtherSessionsCommandHandler(
    IRefreshTokenSessionWriteRepository repository,
    TimeProvider timeProvider) : ICommandHandler<RevokeOtherSessionsCommand, Result> {
    public async Task<Result> Handle(RevokeOtherSessionsCommand command, CancellationToken cancellationToken) {
        await repository.RevokeAllOtherAsync(
            new UserId(command.UserId),
            command.CurrentSessionId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
