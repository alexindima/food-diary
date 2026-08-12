using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    ITelegramAuthValidator telegramAuthValidator,
    ITelegramAssertionReplayGuard replayGuard) : ICommandHandler<LinkTelegramCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(LinkTelegramCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<UserModel>(userIdResult);
        }

        Result<TelegramInitData> initDataResult = telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<UserModel>(initDataResult.Error);
        }

        TelegramInitData initData = initDataResult.Value;
        bool consumed = await replayGuard
            .TryConsumeAsync(command.InitData, initData.AuthDateUtc.AddDays(1), cancellationToken)
            .ConfigureAwait(false);
        if (!consumed) {
            return Result.Failure<UserModel>(Errors.Authentication.TelegramAssertionAlreadyUsed);
        }

        return await userIdentityService
            .LinkTelegramAsync(userIdResult.Value, initData.UserId, cancellationToken)
            .ConfigureAwait(false);
    }
}
