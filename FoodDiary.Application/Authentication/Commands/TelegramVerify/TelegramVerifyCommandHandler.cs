using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandHandler : ICommandHandler<TelegramVerifyCommand, Result<AuthenticationModel>> {
    private readonly IUserRepository _userRepository;
    private readonly ITelegramAuthValidator _telegramAuthValidator;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public TelegramVerifyCommandHandler(
        IUserRepository userRepository,
        ITelegramAuthValidator telegramAuthValidator,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _telegramAuthValidator = telegramAuthValidator;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationModel>> Handle(TelegramVerifyCommand command, CancellationToken cancellationToken) {
        var initDataResult = _telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(initDataResult.Error);
        }

        var initData = initDataResult.Value;
        var user = await _userRepository.GetByTelegramUserIdAsync(initData.UserId, cancellationToken);
        var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramNotLinked);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken, command.ClientContext);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
