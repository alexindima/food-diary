using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RegisterTelegramOperation;

public sealed class RegisterTelegramOperationCommandHandler(ITelegramOperationStore store, ITelegramOperationPolicy policy,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) : ICommandHandler<RegisterTelegramOperationCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(RegisterTelegramOperationCommand command, CancellationToken cancellationToken) {
        long updateId = command.UpdateId;
        long telegramUserId = command.TelegramUserId;
        string payload = command.Payload;
        if (!TelegramOperationChecks.IsEnabled(policy)) {
            return Result.Failure<Guid>(TelegramOperationChecks.Unavailable);
        }
        if (updateId < 0 || telegramUserId <= 0 || !TelegramOperationChecks.ValidPayload(payload)) {
            return Result.Failure<Guid>(TelegramOperationChecks.Invalid);
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(telegramUserId,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure) {
            return Result.Failure<Guid>(principal.Error);
        }
        Guid? id = await store.RegisterAsync(policy.BotId, updateId, principal.Value.UserId.Value,
            principal.Value.SecurityVersion, payload, cancellationToken).ConfigureAwait(false);
        return id.HasValue ? Result.Success(id.Value) : Result.Failure<Guid>(TelegramOperationChecks.Conflict);
    }
}
