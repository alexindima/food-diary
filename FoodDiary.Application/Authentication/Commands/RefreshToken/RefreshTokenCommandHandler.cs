using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using System.Linq;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<string>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var validationResult = _jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null)
        {
            return Result.Failure<string>(Errors.Authentication.InvalidToken);
        }

        var (userId, email) = validationResult.Value;
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.RefreshToken == null)
        {
            return Result.Failure<string>(Errors.Authentication.InvalidToken);
        }

        if (!_passwordHasher.Verify(command.RefreshToken, user.RefreshToken))
        {
            return Result.Failure<string>(Errors.Authentication.InvalidToken);
        }

        var roles = user.UserRoles
            .Select(role => role.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(userId, email, roles);
        return Result.Success(newAccessToken);
    }
}
