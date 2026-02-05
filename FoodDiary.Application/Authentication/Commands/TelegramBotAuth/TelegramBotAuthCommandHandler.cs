using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;
using System.Linq;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed class TelegramBotAuthCommandHandler : ICommandHandler<TelegramBotAuthCommand, Result<AuthenticationResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public TelegramBotAuthCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthenticationResponse>> Handle(TelegramBotAuthCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByTelegramUserIdAsync(command.TelegramUserId);
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
