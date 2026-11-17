using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Services;

public sealed class PasswordAuthenticationService(IUserAuthenticationIdentityService users, TimeProvider clock, IPasswordLoginThrottle throttle) {
    public async Task<Result<UserAuthenticationPrincipalModel>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken) {
        PasswordLoginAttempt attempt = await throttle.TryBeginAsync(email, cancellationToken);
        if (!attempt.IsAcquired) {
            return Result.Failure<UserAuthenticationPrincipalModel>(IdentityErrors.PasswordLoginThrottled(attempt.RetryAfterSeconds));
        }

        bool completed = false;
        try {
            Result<UserAuthenticationPrincipalModel> result = await users
                .AuthenticatePasswordAsync(email, password, clock.GetUtcNow().UtcDateTime, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            PasswordLoginAttemptOutcome outcome = PasswordLoginAttemptOutcome.Abandoned;
            if (result.IsSuccess) {
                outcome = PasswordLoginAttemptOutcome.Succeeded;
            } else if (string.Equals(result.Error.Code, AuthenticationErrors.InvalidCredentials.Code, StringComparison.Ordinal)) {
                outcome = PasswordLoginAttemptOutcome.InvalidCredentials;
            }
            bool owned = await throttle.CompleteAsync(attempt, outcome, cancellationToken);
            completed = true;
            return owned ? result : Result.Failure<UserAuthenticationPrincipalModel>(IdentityErrors.PasswordLoginThrottled(1));
        } finally {
            if (!completed) {
                using var cleanupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await throttle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.Abandoned, cleanupCancellation.Token);
            }
        }
    }
}
