using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TelegramVerify;

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
            return Result.Failure<AuthenticationModel>(IdentityErrors.TelegramAssertionAlreadyUsed);
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
