using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RestoreAccount;

public sealed class RestoreAccountCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<RestoreAccountCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RestoreAccountCommand command, CancellationToken cancellationToken) {
        DateTime restoredAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        Result<UserAuthenticationPrincipalModel> restoreResult = await userIdentityService
            .RestoreAccountAsync(
                command.Email,
                command.Password,
                restoredAtUtc,
                cancellationToken)
            .ConfigureAwait(false);
        if (restoreResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(restoreResult.Error);
        }

        UserAuthenticationPrincipalModel principal = restoreResult.Value;
        await refreshTokenSessionRepository
            .RevokeAllAsync(principal.UserId, restoredAtUtc, cancellationToken)
            .ConfigureAwait(false);
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext, command.RememberMe)
            .ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }
}
