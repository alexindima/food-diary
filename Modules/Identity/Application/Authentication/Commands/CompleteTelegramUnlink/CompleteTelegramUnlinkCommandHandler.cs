using System.Text.Json;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CompleteTelegramUnlink;

public sealed class CompleteTelegramUnlinkCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    IUserAuthenticationIdentityService identities, IUserTelegramAccountService accounts, TimeProvider timeProvider,
    ITelegramOperationStore operations, IPostCommitActionQueue postCommitActions) : ICommandHandler<CompleteTelegramUnlinkCommand, Result> {
    public async Task<Result> Handle(CompleteTelegramUnlinkCommand command, CancellationToken cancellationToken) {
        Guid userId = command.UserId ?? Guid.Empty;
        if (userId == Guid.Empty || command.BrowserBinding.Length != 43 || !provider.IsEnabled) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        string? json = await tickets.ConsumeAsync(command.State, TelegramUnlinkOidcAttempt.Purpose,
            TelegramUnlinkOidcAttempt.Binding(command.BrowserBinding, userId), cancellationToken);
        if (json is null) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        TelegramUnlinkOidcAttempt? attempt;
        try {
            attempt = JsonSerializer.Deserialize<TelegramUnlinkOidcAttempt>(json);
        } catch (JsonException) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        if (attempt is null || attempt.UserId != userId) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        Result<TelegramOidcIdentity> identity = await provider.ExchangeAsync(new TelegramOidcTokenExchange(
            new TelegramAuthorizationCode(command.Code), new TelegramPkceVerifier(attempt.CodeVerifier), new TelegramOidcNonce(attempt.Nonce)), cancellationToken);
        if (identity.IsFailure) {
            return Result.Failure(identity.Error);
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(
            identity.Value.TelegramUserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (principal.IsFailure || principal.Value.UserId.Value != userId || principal.Value.SecurityVersion != attempt.SecurityVersion) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        Result result = await accounts.UnlinkAsync(principal.Value.UserId, identity.Value.TelegramUserId, attempt.SecurityVersion, cancellationToken);
        if (result.IsSuccess) {
            postCommitActions.Enqueue("auth.telegram.unlink.cancel-operations", ct => operations.CancelUserAsync(principal.Value.UserId, ct));
        }
        return result;
    }
}
