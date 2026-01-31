using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Users;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandHandler : ICommandHandler<LinkTelegramCommand, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITelegramAuthValidator _telegramAuthValidator;

    public LinkTelegramCommandHandler(
        IUserRepository userRepository,
        ITelegramAuthValidator telegramAuthValidator)
    {
        _userRepository = userRepository;
        _telegramAuthValidator = telegramAuthValidator;
    }

    public async Task<Result<UserResponse>> Handle(LinkTelegramCommand command, CancellationToken cancellationToken)
    {
        var initDataResult = _telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess)
        {
            return Result.Failure<UserResponse>(initDataResult.Error);
        }

        var initData = initDataResult.Value;
        var currentUser = await _userRepository.GetByIdAsync(command.UserId);
        if (currentUser == null)
        {
            return Result.Failure<UserResponse>(Errors.User.NotFound());
        }

        if (currentUser.TelegramUserId == initData.UserId)
        {
            return Result.Success(currentUser.ToResponse());
        }

        var existingUser = await _userRepository.GetByTelegramUserIdIncludingDeletedAsync(initData.UserId);
        if (existingUser != null && existingUser.Id != currentUser.Id)
        {
            return Result.Failure<UserResponse>(Errors.Authentication.TelegramAlreadyLinked);
        }

        currentUser.LinkTelegram(initData.UserId);
        await _userRepository.UpdateAsync(currentUser);

        return Result.Success(currentUser.ToResponse());
    }
}
