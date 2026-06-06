using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandHandler(
    IUserRepository userRepository,
    ITelegramAuthValidator telegramAuthValidator) : ICommandHandler<LinkTelegramCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(LinkTelegramCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        Result<TelegramInitData> initDataResult = telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<UserModel>(initDataResult.Error);
        }

        TelegramInitData initData = initDataResult.Value;
        User? currentUser = await userRepository.GetByIdAsync(new UserId(command.UserId), cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(currentUser);
        if (accessError is not null) {
            return Result.Failure<UserModel>(accessError);
        }

        User currentAccessibleUser = currentUser!;

        if (currentAccessibleUser.TelegramUserId == initData.UserId) {
            return Result.Success(currentAccessibleUser.ToModel());
        }

        User? existingUser = await userRepository.GetByTelegramUserIdIncludingDeletedAsync(initData.UserId, cancellationToken).ConfigureAwait(false);
        if (existingUser != null && existingUser.Id != currentAccessibleUser.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.TelegramAlreadyLinked);
        }

        currentAccessibleUser.LinkTelegram(initData.UserId);
        await userRepository.UpdateAsync(currentAccessibleUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(currentAccessibleUser.ToModel());
    }
}
