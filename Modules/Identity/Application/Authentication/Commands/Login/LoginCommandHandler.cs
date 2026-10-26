using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService,
    IPasswordLoginThrottle passwordLoginThrottle) : ICommandHandler<LoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(LoginCommand command, CancellationToken cancellationToken) {
        PasswordLoginAttempt attempt = await passwordLoginThrottle.TryBeginAsync(command.Email, cancellationToken).ConfigureAwait(false);
        if (!attempt.IsAcquired) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.PasswordLoginThrottled(attempt.RetryAfterSeconds));
        }

        bool completed = false;
        try {
            Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
                .AuthenticatePasswordAsync(
                    command.Email,
                    command.Password,
                    dateTimeProvider.GetUtcNow().UtcDateTime,
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            PasswordLoginAttemptOutcome outcome = PasswordLoginAttemptOutcome.Abandoned;
            if (authenticationResult.IsSuccess) {
                outcome = PasswordLoginAttemptOutcome.Succeeded;
            } else if (string.Equals(authenticationResult.Error.Code, AuthenticationErrors.InvalidCredentials.Code, StringComparison.Ordinal)) {
                outcome = PasswordLoginAttemptOutcome.InvalidCredentials;
            }
            bool owned = await passwordLoginThrottle.CompleteAsync(attempt, outcome, cancellationToken).ConfigureAwait(false);
            completed = true;
            if (!owned) {
                return Result.Failure<AuthenticationModel>(IdentityErrors.PasswordLoginThrottled(1));
            }
            if (authenticationResult.IsFailure) {
                return Result.Failure<AuthenticationModel>(authenticationResult.Error);
            }

            UserAuthenticationPrincipalModel principal = authenticationResult.Value;
            IssuedAuthenticationTokens tokens = await authenticationTokenService
                .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext, command.RememberMe)
                .ConfigureAwait(false);
            return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User, tokens.BrowserSession));
        } finally {
            if (!completed) {
                using var cleanupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await passwordLoginThrottle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.Abandoned, cleanupCancellation.Token)
                    .ConfigureAwait(false);
            }
        }
    }
}
