using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RestoreAccount;

public sealed class RestoreAccountCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IAuthenticationTokenService authenticationTokenService,
    IPasswordLoginThrottle passwordLoginThrottle)
    : ICommandHandler<RestoreAccountCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RestoreAccountCommand command, CancellationToken cancellationToken) {
        DateTime restoredAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        PasswordLoginAttempt attempt = await passwordLoginThrottle.TryBeginAsync(command.Email, cancellationToken);
        if (!attempt.IsAcquired) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.PasswordLoginThrottled(attempt.RetryAfterSeconds));
        }

        Result<UserAuthenticationPrincipalModel> restoreResult;
        bool completed = false;
        try {
            restoreResult = await userIdentityService.RestoreAccountAsync(
                command.Email, command.Password, restoredAtUtc, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            PasswordLoginAttemptOutcome outcome = restoreResult.IsSuccess
                ? PasswordLoginAttemptOutcome.Succeeded
                : PasswordLoginAttemptOutcome.Abandoned;
            if (restoreResult.IsFailure &&
                string.Equals(restoreResult.Error.Code, AuthenticationErrors.InvalidCredentials.Code, StringComparison.Ordinal)) {
                outcome = PasswordLoginAttemptOutcome.InvalidCredentials;
            }

            bool owned = await passwordLoginThrottle.CompleteAsync(attempt, outcome, cancellationToken);
            completed = true;
            if (!owned) {
                return Result.Failure<AuthenticationModel>(IdentityErrors.PasswordLoginThrottled(1));
            }
        } finally {
            if (!completed) {
                using var cleanupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await passwordLoginThrottle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.Abandoned, cleanupCancellation.Token);
            }
        }

        if (restoreResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(restoreResult.Error);
        }

        UserAuthenticationPrincipalModel principal = restoreResult.Value;
        await refreshTokenSessionRepository
            .RevokeAllAsync(principal.UserId, restoredAtUtc, cancellationToken);
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext, command.RememberMe);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User, tokens.BrowserSession));
    }
}
