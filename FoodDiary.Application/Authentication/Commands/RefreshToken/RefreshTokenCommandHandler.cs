using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IAuthenticationUserLookupService userLookupService,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher,
    IRefreshTokenSessionRepository refreshTokenSessionRepository,
    IAuthenticationTokenService authenticationTokenService,
    TimeProvider dateTimeProvider) : ICommandHandler<RefreshTokenCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) {
        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validationResult = jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        (UserId userId, string _, bool rememberMe, Guid? refreshSessionId) = validationResult.Value;
        if (!refreshSessionId.HasValue) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        User? user = await userLookupService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        UserRefreshTokenSession? session = await refreshTokenSessionRepository.GetByIdAsync(refreshSessionId.Value, cancellationToken).ConfigureAwait(false);
        if (session is null || session.UserId != userId || !session.IsActive) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        if (!IsRefreshTokenValid(command.RefreshToken, session, nowUtc)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        User currentUser = user!;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueAndStoreAsync(currentUser, cancellationToken, rememberMe: rememberMe, refreshSessionId: refreshSessionId)
            .ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }

    private bool IsRefreshTokenValid(string refreshToken, UserRefreshTokenSession session, DateTime nowUtc) =>
        VerifyRefreshToken(refreshToken, session.RefreshTokenHash) ||
        IsPreviousRefreshTokenValid(refreshToken, session, nowUtc);

    private bool IsPreviousRefreshTokenValid(string refreshToken, UserRefreshTokenSession session, DateTime nowUtc) =>
        session.PreviousRefreshTokenHash is { Length: > 0 } previousHash &&
        session.PreviousRefreshTokenValidUntilUtc is { } validUntilUtc &&
        validUntilUtc > nowUtc &&
        VerifyRefreshToken(refreshToken, previousHash);

    private bool VerifyRefreshToken(string refreshToken, string refreshTokenHash) =>
        SecurityTokenGenerator.IsFastStorageHash(refreshTokenHash)
            ? SecurityTokenGenerator.VerifyFastStorageHash(refreshToken, refreshTokenHash)
            : passwordHasher.Verify(SecurityTokenGenerator.NormalizeForSecureHashing(refreshToken), refreshTokenHash);
}
