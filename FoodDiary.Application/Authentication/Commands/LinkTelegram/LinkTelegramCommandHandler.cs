using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandHandler : ICommandHandler<LinkTelegramCommand, Result<UserModel>> {
    private readonly IUserRepository _userRepository;
    private readonly ITelegramAuthValidator _telegramAuthValidator;

    public LinkTelegramCommandHandler(
        IUserRepository userRepository,
        ITelegramAuthValidator telegramAuthValidator) {
        _userRepository = userRepository;
        _telegramAuthValidator = telegramAuthValidator;
    }

    public async Task<Result<UserModel>> Handle(LinkTelegramCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var initDataResult = _telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<UserModel>(initDataResult.Error);
        }

        var initData = initDataResult.Value;
        var currentUser = await _userRepository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(currentUser);
        if (accessError is not null) {
            return Result.Failure<UserModel>(accessError);
        }

        var currentAccessibleUser = currentUser!;

        if (currentAccessibleUser.TelegramUserId == initData.UserId) {
            return Result.Success(currentAccessibleUser.ToModel());
        }

        var existingUser = await _userRepository.GetByTelegramUserIdIncludingDeletedAsync(initData.UserId, cancellationToken);
        if (existingUser != null && existingUser.Id != currentAccessibleUser.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.TelegramAlreadyLinked);
        }

        currentAccessibleUser.LinkTelegram(initData.UserId);
        await _userRepository.UpdateAsync(currentAccessibleUser, cancellationToken);

        return Result.Success(currentAccessibleUser.ToModel());
    }
}
