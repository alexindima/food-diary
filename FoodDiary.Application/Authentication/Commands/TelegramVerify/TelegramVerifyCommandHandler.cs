using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;
using System.Linq;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandHandler : ICommandHandler<TelegramVerifyCommand, Result<AuthenticationResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITelegramAuthValidator _telegramAuthValidator;

    public TelegramVerifyCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        ITelegramAuthValidator telegramAuthValidator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _telegramAuthValidator = telegramAuthValidator;
    }

    public async Task<Result<AuthenticationResponse>> Handle(TelegramVerifyCommand command, CancellationToken cancellationToken)
    {
        var initDataResult = _telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess)
        {
            return Result.Failure<AuthenticationResponse>(initDataResult.Error);
        }

        var initData = initDataResult.Value;
        var user = await _userRepository.GetByTelegramUserIdAsync(initData.UserId);
        if (user == null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.TelegramNotLinked);
        }

        if (user.DeletedAt is not null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        var roles = user.UserRoles
            .Select(role => role.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);
        var hashedRefreshToken = _passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await _userRepository.UpdateAsync(user);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(accessToken, refreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
