using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    ITelegramAuthValidator telegramAuthValidator,
    ITelegramAssertionReplayGuard replayGuard,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<TelegramVerifyCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(TelegramVerifyCommand command, CancellationToken cancellationToken) {
        Result<TelegramInitData> initDataResult = telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(initDataResult.Error);
        }

        TelegramInitData initData = initDataResult.Value;
        bool consumed = await replayGuard
            .TryConsumeAsync(command.InitData, initData.AuthDateUtc.AddDays(1), cancellationToken)
            .ConfigureAwait(false);
        if (!consumed) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramAssertionAlreadyUsed);
        }

        Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
            .AuthenticateTelegramAsync(
                initData.UserId,
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
