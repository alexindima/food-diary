using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;
using System.Linq;

namespace FoodDiary.Application.Authentication.Commands.Login;

public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthenticationResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthenticationResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailIncludingDeletedAsync(command.Email);
        if (user == null || !_passwordHasher.Verify(command.Password, user.Password))
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is not null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        // Создание токенов
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
