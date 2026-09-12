using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Commands.StartTelegramOidc;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.ExchangeTelegramOidc;

public sealed class ExchangeTelegramOidcCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    ITelegramIdentityPolicy policy, TelegramAuthenticationIntentService intents) : ICommandHandler<ExchangeTelegramOidcCommand, Result<TelegramAuthenticationIntentModel>> {
    public async Task<Result<TelegramAuthenticationIntentModel>> Handle(ExchangeTelegramOidcCommand command, CancellationToken cancellationToken) {
        if (!policy.LoginEnabled) {
            return Result.Failure<TelegramAuthenticationIntentModel>(TelegramIdentityErrors.NotConfigured);
        }
        string? json = await tickets.ConsumeAsync(command.State, StartTelegramOidcCommandHandler.Purpose, command.BrowserBinding, cancellationToken).ConfigureAwait(false);
        if (json is null) {
            return Invalid();
        }
        StartTelegramOidcCommandHandler.OidcAttempt? attempt;
        try {
            attempt = JsonSerializer.Deserialize<StartTelegramOidcCommandHandler.OidcAttempt>(json);
        } catch (JsonException) {
            return Invalid();
        }
        if (attempt is null) {
            return Invalid();
        }
        Result<TelegramOidcIdentity> identity = await provider.ExchangeAsync(command.Code, attempt.CodeVerifier, attempt.Nonce, cancellationToken).ConfigureAwait(false);
        if (identity.IsFailure) {
            return Result.Failure<TelegramAuthenticationIntentModel>(identity.Error);
        }
        return await intents.CreateAsync(identity.Value.TelegramUserId, identity.Value.FirstName, identity.Value.LastName,
            language: null, command.BrowserBinding, attempt.LinkUserId, cancellationToken,
            identity.Value.Issuer, identity.Value.Subject).ConfigureAwait(false);
    }
    private static Result<TelegramAuthenticationIntentModel> Invalid() => Result.Failure<TelegramAuthenticationIntentModel>(TelegramIdentityErrors.InvalidProof);
}
