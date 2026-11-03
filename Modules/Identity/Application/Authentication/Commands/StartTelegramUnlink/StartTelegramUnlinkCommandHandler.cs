using System.Text.Json;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramUnlink;

public sealed class StartTelegramUnlinkCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) : ICommandHandler<StartTelegramUnlinkCommand, Result<TelegramOidcStartModel>> {
    public async Task<Result<TelegramOidcStartModel>> Handle(StartTelegramUnlinkCommand command, CancellationToken cancellationToken) {
        Guid userId = command.UserId ?? Guid.Empty;
        if (userId == Guid.Empty || command.BrowserBinding.Length != 43) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.InvalidProof);
        }
        if (!provider.IsEnabled) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.NotConfigured);
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Result<UserAuthenticationPrincipalModel> principal = await identities.GetAuthenticationPrincipalAsync(new UserId(userId), now, cancellationToken);
        if (principal.IsFailure || !principal.Value.User.HasTelegramIdentity) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.InvalidProof);
        }
        var attempt = new TelegramUnlinkOidcAttempt(userId, principal.Value.SecurityVersion,
            SecurityTokenGenerator.GenerateUrlSafeToken(), SecurityTokenGenerator.GenerateUrlSafeToken());
        string state = await tickets.CreateAsync(TelegramUnlinkOidcAttempt.Purpose,
            TelegramUnlinkOidcAttempt.Binding(command.BrowserBinding, userId), JsonSerializer.Serialize(attempt), now.AddMinutes(5), cancellationToken);
        Result<string> url = provider.CreateAuthorizationUrl(new TelegramOidcAuthorizationRequest(
            new TelegramOAuthState(state), new TelegramOidcNonce(attempt.Nonce), new TelegramPkceVerifier(attempt.CodeVerifier)));
        return url.IsSuccess ? Result.Success(new TelegramOidcStartModel(url.Value)) : Result.Failure<TelegramOidcStartModel>(url.Error);
    }
}
