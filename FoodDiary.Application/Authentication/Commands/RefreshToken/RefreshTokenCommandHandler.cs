using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<AuthenticationModel>> {
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

    public async Task<Result<AuthenticationModel>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) {
        var validationResult = _jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var (userId, _) = validationResult.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        var normalizedRefreshToken = SecurityTokenGenerator.NormalizeForSecureHashing(command.RefreshToken);
        if (user?.RefreshToken is null || !_passwordHasher.Verify(normalizedRefreshToken, user.RefreshToken)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
