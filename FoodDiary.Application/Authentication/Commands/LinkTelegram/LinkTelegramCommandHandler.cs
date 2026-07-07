using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandHandler(
    IUserContextService userContextService,
    IAuthenticationUserMutationService userMutationService,
    ITelegramAuthValidator telegramAuthValidator) : ICommandHandler<LinkTelegramCommand, Result<UserModel>> {
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
        Result<User> currentUserResult = await userContextService
            .GetAccessibleUserAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!currentUserResult.IsSuccess) {
            return Result.Failure<UserModel>(currentUserResult.Error);
        }

        User currentAccessibleUser = currentUserResult.Value;

        if (currentAccessibleUser.TelegramUserId == initData.UserId) {
            return Result.Success(currentAccessibleUser.ToModel());
        }

        User? existingUser = await userMutationService.GetByTelegramUserIdIncludingDeletedAsync(initData.UserId, cancellationToken).ConfigureAwait(false);
        if (existingUser != null && existingUser.Id != currentAccessibleUser.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.TelegramAlreadyLinked);
        }

        currentAccessibleUser.LinkTelegram(initData.UserId);
        await userContextService.UpdateUserAsync(currentAccessibleUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(currentAccessibleUser.ToModel());
    }
}
