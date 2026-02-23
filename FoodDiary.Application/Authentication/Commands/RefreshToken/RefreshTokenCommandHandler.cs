using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<string>> {
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<string>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) {
        var validationResult = _jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null) {
            return Result.Failure<string>(Errors.Authentication.InvalidToken);
        }

        var (userId, _) = validationResult.Value;
        var user = await _userRepository.GetByIdAsync(userId);

        if (user?.RefreshToken is null || !_passwordHasher.Verify(command.RefreshToken, user.RefreshToken)) {
            return Result.Failure<string>(Errors.Authentication.InvalidToken);
        }

        var newAccessToken = _authenticationTokenService.IssueAccessToken(user);
        return Result.Success(newAccessToken);
    }
}
