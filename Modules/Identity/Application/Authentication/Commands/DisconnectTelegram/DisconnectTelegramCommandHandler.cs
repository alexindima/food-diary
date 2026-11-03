using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;

public sealed class DisconnectTelegramCommandHandler(IUserTelegramAccountService accounts,
    ITelegramOperationStore operations, IPostCommitActionQueue postCommitActions) : ICommandHandler<DisconnectTelegramCommand, Result> {
    public async Task<Result> Handle(DisconnectTelegramCommand command, CancellationToken cancellationToken) {
        if (command.UserId is not { } id || id == Guid.Empty || command.SecurityVersion is not >= 0) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        var userId = new UserId(id);
        Result result = await accounts.DisconnectAsync(userId, command.SecurityVersion.Value, cancellationToken);
        if (result.IsSuccess) {
            postCommitActions.Enqueue("auth.telegram.disconnect.cancel-operations", ct => operations.CancelUserAsync(userId, ct));
        }
        return result;
    }
}
