using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Text.Json;
using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Services;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CompleteTelegramBackupEmail;

public sealed class CompleteTelegramBackupEmailCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    TelegramBackupEmailService backupEmail) : ICommandHandler<CompleteTelegramBackupEmailCommand, Result> {
    public async Task<Result> Handle(CompleteTelegramBackupEmailCommand command, CancellationToken cancellationToken) {
        Guid userId = command.UserId ?? Guid.Empty;
        string code = command.Code;
        string state = command.State;
        string browserBinding = command.BrowserBinding;
        if (userId == Guid.Empty || browserBinding.Length != 43 || !provider.IsEnabled) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        string? json = await tickets.ConsumeAsync(state, TelegramBackupEmailOidcAttempt.Purpose, TelegramBackupEmailOidcAttempt.Binding(browserBinding, userId), cancellationToken).ConfigureAwait(false);
        if (json is null) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        TelegramBackupEmailOidcAttempt? attempt;
        try {
            attempt = JsonSerializer.Deserialize<TelegramBackupEmailOidcAttempt>(json);
        } catch (JsonException) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        if (attempt is null || attempt.UserId != userId) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        Result<TelegramOidcIdentity> identity = await provider.ExchangeAsync(code, attempt.CodeVerifier, attempt.Nonce, cancellationToken).ConfigureAwait(false);
        if (identity.IsFailure) {
            return Result.Failure(identity.Error);
        }
        return await backupEmail.RequestOidcAsync(userId, attempt.Email, identity.Value.TelegramUserId,
            attempt.SecurityVersion, cancellationToken).ConfigureAwait(false);
    }
}
