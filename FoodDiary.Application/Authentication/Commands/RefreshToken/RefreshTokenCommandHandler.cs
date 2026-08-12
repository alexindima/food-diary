using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
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

        UserRefreshTokenSession? session = await refreshTokenSessionRepository
            .GetByIdAsync(refreshSessionId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.UserId != userId || !session.IsActive) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        if (!VerifyRefreshToken(command.RefreshToken, session.RefreshTokenHash)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
            .RecordAuthenticationAsync(
                userId,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(
                principal,
                cancellationToken,
                rememberMe: rememberMe,
                refreshSessionId: refreshSessionId)
            .ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }

    private bool VerifyRefreshToken(string refreshToken, string refreshTokenHash) =>
        SecurityTokenGenerator.IsFastStorageHash(refreshTokenHash)
            ? SecurityTokenGenerator.VerifyFastStorageHash(refreshToken, refreshTokenHash)
            : passwordHasher.Verify(SecurityTokenGenerator.NormalizeForSecureHashing(refreshToken), refreshTokenHash);
}
