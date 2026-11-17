using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;

public sealed class DisconnectTelegramCommandHandler(
    IUserTelegramAccountService accounts,
    IUserAuthenticationIdentityService users,
    PasswordAuthenticationService passwords,
    IGoogleTokenValidator google,
    IAuthenticationTokenService tokens,
    ITelegramOperationStore operations,
    IPostCommitActionQueue postCommitActions,
    TimeProvider clock) : ICommandHandler<DisconnectTelegramCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(DisconnectTelegramCommand command, CancellationToken cancellationToken) {
        if (command.UserId is not { } id || id == Guid.Empty || command.SecurityVersion is not >= 0) {
            return Result.Failure<AuthenticationModel>(TelegramIdentityErrors.InvalidProof);
        }

        var userId = new UserId(id);
        Result<UserAuthenticationPrincipalModel> confirmation = await ConfirmAsync(command, userId, cancellationToken);
        if (confirmation.IsFailure) {
            return Result.Failure<AuthenticationModel>(confirmation.Error);
        }
        if (confirmation.Value.UserId != userId || confirmation.Value.SecurityVersion != command.SecurityVersion.Value) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.ReauthenticationAccountMismatch);
        }

        Result<UserAuthenticationPrincipalModel> disconnected = await accounts.DisconnectAsync(userId, command.SecurityVersion.Value, cancellationToken);
        if (disconnected.IsFailure) {
            return Result.Failure<AuthenticationModel>(disconnected.Error);
        }

        string provider = command.Password is not null ? "password" : "google";
        // The owner already revoked all prior sessions in this transaction.
        AuthenticationClientContext replacementContext = command.ClientContext with {
            AuthProvider = provider,
            ExistingRefreshToken = null,
            ExistingBrowserCredential = null,
            PreviousBrowserSessionId = null,
        };
        IssuedAuthenticationTokens issued = await tokens.IssueFromPrincipalAsync(
            disconnected.Value, cancellationToken, replacementContext, command.RememberMe);
        postCommitActions.Enqueue("auth.telegram.disconnect.cancel-operations", ct => operations.CancelUserAsync(userId, ct));
        return Result.Success(new AuthenticationModel(issued.AccessToken, issued.RefreshToken, disconnected.Value.User, issued.BrowserSession));
    }

    private async Task<Result<UserAuthenticationPrincipalModel>> ConfirmAsync(
        DisconnectTelegramCommand command, UserId userId, CancellationToken cancellationToken) {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        if (command.Password is { Length: > 0 } password && command.GoogleCredential is null) {
            Result<UserAuthenticationPrincipalModel> current = await users.GetAuthenticationPrincipalAsync(userId, now, cancellationToken);
            if (current.IsFailure) {
                return current;
            }
            if (current.Value.User is not { HasPassword: true, IsEmailConfirmed: true, Email: { } email }) {
                return Result.Failure<UserAuthenticationPrincipalModel>(IdentityErrors.ReauthenticationRequired);
            }
            return await passwords.AuthenticateAsync(email, password, cancellationToken);
        }
        if (command.GoogleCredential is { Length: > 0 } credential && command.Password is null) {
            Result<GoogleIdentityPayload> validated = await google.ValidateCredentialAsync(credential, cancellationToken);
            if (validated.IsFailure) {
                return Result.Failure<UserAuthenticationPrincipalModel>(validated.Error);
            }
            GoogleIdentityPayload identity = validated.Value;
            return await users.AuthenticateLinkedGoogleAsync(
                new UserGoogleAuthenticationModel(identity.Issuer, identity.Subject, identity.Email, identity.FirstName, identity.LastName, identity.Locale),
                now, cancellationToken);
        }
        return Result.Failure<UserAuthenticationPrincipalModel>(IdentityErrors.ReauthenticationRequired);
    }
}
