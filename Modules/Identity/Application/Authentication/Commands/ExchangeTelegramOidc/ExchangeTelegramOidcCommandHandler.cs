using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Text.Json;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramOidc;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ExchangeTelegramOidc;

public sealed class ExchangeTelegramOidcCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    ITelegramIdentityPolicy policy, TelegramAuthenticationIntentService intents,
    ILogger<ExchangeTelegramOidcCommandHandler> logger) : ICommandHandler<ExchangeTelegramOidcCommand, Result<TelegramAuthenticationIntentModel>> {
    public async Task<Result<TelegramAuthenticationIntentModel>> Handle(ExchangeTelegramOidcCommand command, CancellationToken cancellationToken) {
        if (!policy.LoginEnabled) {
            LogRejection("not_configured");
            return Result.Failure<TelegramAuthenticationIntentModel>(TelegramIdentityErrors.NotConfigured);
        }
        string? json = await tickets.ConsumeAsync(command.State, StartTelegramOidcCommandHandler.Purpose, command.BrowserBinding, cancellationToken).ConfigureAwait(false);
        if (json is null) {
            return Invalid("attempt_unavailable");
        }
        StartTelegramOidcCommandHandler.OidcAttempt? attempt;
        try {
            attempt = JsonSerializer.Deserialize<StartTelegramOidcCommandHandler.OidcAttempt>(json);
        } catch (JsonException) {
            return Invalid("attempt_payload_invalid");
        }
        if (attempt is null) {
            return Invalid("attempt_payload_invalid");
        }
        Result<TelegramOidcIdentity> identity = await provider.ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode(command.Code), new TelegramPkceVerifier(attempt.CodeVerifier), new TelegramOidcNonce(attempt.Nonce)), cancellationToken).ConfigureAwait(false);
        if (identity.IsFailure) {
            return Result.Failure<TelegramAuthenticationIntentModel>(identity.Error);
        }
        return await intents.CreateAsync(identity.Value.TelegramUserId, identity.Value.FirstName, identity.Value.LastName,
            language: null, command.BrowserBinding, attempt.LinkUserId, cancellationToken,
            identity.Value.Issuer, identity.Value.Subject).ConfigureAwait(false);
    }
    private Result<TelegramAuthenticationIntentModel> Invalid(string reason) {
        LogRejection(reason);
        return Result.Failure<TelegramAuthenticationIntentModel>(TelegramIdentityErrors.InvalidProof);
    }

    private void LogRejection(string reason) =>
        logger.LogInformation("Telegram OIDC rejected. Stage={Stage}; Reason={Reason}", "attempt", reason);
}
