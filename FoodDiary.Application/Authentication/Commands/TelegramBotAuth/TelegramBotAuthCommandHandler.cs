using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed class TelegramBotAuthCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<TelegramBotAuthCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(TelegramBotAuthCommand command, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
            .AuthenticateTelegramAsync(
                command.TelegramUserId,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(authenticationResult.Error);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext)
            .ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }
}
