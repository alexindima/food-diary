using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.StartTelegramOidc;

public sealed class StartTelegramOidcCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    ITelegramIdentityPolicy policy, TimeProvider timeProvider) : ICommandHandler<StartTelegramOidcCommand, Result<TelegramOidcStartModel>> {
    internal const string Purpose = "telegram-oidc-attempt";
    public async Task<Result<TelegramOidcStartModel>> Handle(StartTelegramOidcCommand command, CancellationToken cancellationToken) {
        if (!policy.LoginEnabled || command.BrowserBinding.Length != 43 || command.LinkUserId == Guid.Empty) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.NotConfigured);
        }
        var attempt = new OidcAttempt(SecurityTokenGenerator.GenerateUrlSafeToken(), SecurityTokenGenerator.GenerateUrlSafeToken(), command.LinkUserId);
        string state = await tickets.CreateAsync(Purpose, command.BrowserBinding, JsonSerializer.Serialize(attempt),
            timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10), cancellationToken).ConfigureAwait(false);
        Result<string> authorization = provider.CreateAuthorizationUrl(state, attempt.Nonce, attempt.CodeVerifier);
        return authorization.IsSuccess ? Result.Success(new TelegramOidcStartModel(authorization.Value)) : Result.Failure<TelegramOidcStartModel>(authorization.Error);
    }

    internal sealed record OidcAttempt(string Nonce, string CodeVerifier, Guid? LinkUserId);
}
