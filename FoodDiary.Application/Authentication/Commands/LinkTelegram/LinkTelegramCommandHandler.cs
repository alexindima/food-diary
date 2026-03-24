using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
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
        var initDataResult = _telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<UserModel>(initDataResult.Error);
        }

        var initData = initDataResult.Value;
        var currentUser = await _userRepository.GetByIdAsync(new UserId(command.UserId));
        if (currentUser == null) {
            return Result.Failure<UserModel>(Errors.User.NotFound());
        }

        if (currentUser.TelegramUserId == initData.UserId) {
            return Result.Success(currentUser.ToModel());
        }

        var existingUser = await _userRepository.GetByTelegramUserIdIncludingDeletedAsync(initData.UserId);
        if (existingUser != null && existingUser.Id != currentUser.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.TelegramAlreadyLinked);
        }

        currentUser.LinkTelegram(initData.UserId);
        await _userRepository.UpdateAsync(currentUser);

        return Result.Success(currentUser.ToModel());
    }
}
