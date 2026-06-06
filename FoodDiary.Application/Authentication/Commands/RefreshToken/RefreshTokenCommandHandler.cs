using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher,
    IRefreshTokenSessionRepository refreshTokenSessionRepository,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<RefreshTokenCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) {
        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validationResult = jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        (UserId userId, string _, bool rememberMe, Guid? refreshSessionId) = validationResult.Value;
        if (!refreshSessionId.HasValue) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        UserRefreshTokenSession? session = await refreshTokenSessionRepository.GetByIdAsync(refreshSessionId.Value, cancellationToken).ConfigureAwait(false);
        if (session is null || session.UserId != userId || !session.IsActive) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        bool isRefreshTokenValid = SecurityTokenGenerator.IsFastStorageHash(session.RefreshTokenHash)
            ? SecurityTokenGenerator.VerifyFastStorageHash(command.RefreshToken, session.RefreshTokenHash)
            : passwordHasher.Verify(SecurityTokenGenerator.NormalizeForSecureHashing(command.RefreshToken), session.RefreshTokenHash);

        if (!isRefreshTokenValid) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        User currentUser = user!;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueAndStoreAsync(currentUser, cancellationToken, rememberMe: rememberMe, refreshSessionId: refreshSessionId)
            .ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }
}
