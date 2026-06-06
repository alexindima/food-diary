using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<AuthenticationModel>> {
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenSessionRepository _refreshTokenSessionRepository;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        IRefreshTokenSessionRepository refreshTokenSessionRepository,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _refreshTokenSessionRepository = refreshTokenSessionRepository;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationModel>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) {
        var validationResult = _jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var (userId, _, rememberMe, refreshSessionId) = validationResult.Value;
        if (!refreshSessionId.HasValue) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var session = await _refreshTokenSessionRepository.GetByIdAsync(refreshSessionId.Value, cancellationToken).ConfigureAwait(false);
        if (session is null || session.UserId != userId || !session.IsActive) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var isRefreshTokenValid = SecurityTokenGenerator.IsFastStorageHash(session.RefreshTokenHash)
            ? SecurityTokenGenerator.VerifyFastStorageHash(command.RefreshToken, session.RefreshTokenHash)
            : _passwordHasher.Verify(SecurityTokenGenerator.NormalizeForSecureHashing(command.RefreshToken), session.RefreshTokenHash);

        if (!isRefreshTokenValid) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var currentUser = user!;
        var tokens = await _authenticationTokenService
            .IssueAndStoreAsync(currentUser, cancellationToken, rememberMe: rememberMe, refreshSessionId: refreshSessionId)
            .ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }
}
