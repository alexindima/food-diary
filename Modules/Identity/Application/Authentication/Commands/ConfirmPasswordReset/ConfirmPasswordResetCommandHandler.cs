using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Identity.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IAuditLogger auditLogger)
    : ICommandHandler<ConfirmPasswordResetCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(ConfirmPasswordResetCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<AuthenticationModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        Result<UserAuthenticationPrincipalModel> resetResult = await userIdentityService
            .CompletePasswordResetAsync(userId, command.Token, command.NewPassword, nowUtc, cancellationToken)
            .ConfigureAwait(false);
        if (resetResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(resetResult.Error);
        }

        await refreshTokenSessionRepository
            .RevokeAllAsync(userId, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        UserAuthenticationPrincipalModel principal = resetResult.Value;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken)
            .ConfigureAwait(false);

        auditLogger.Log("auth.password-reset.confirm", userId, "User", userId.Value.ToString());

        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }
}
