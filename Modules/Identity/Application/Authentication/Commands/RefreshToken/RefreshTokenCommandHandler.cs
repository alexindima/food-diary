using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<RefreshTokenCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) {
        (UserId userId, string? email, bool rememberMe, Guid? refreshSessionId)? validationResult = jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult == null) {
            return Result.Failure<AuthenticationModel>(AuthenticationErrors.InvalidToken);
        }

        (UserId userId, string? _, bool rememberMe, Guid? refreshSessionId) = validationResult.Value;
        if (!refreshSessionId.HasValue) {
            return Result.Failure<AuthenticationModel>(AuthenticationErrors.InvalidToken);
        }

        UserRefreshTokenSession? session = await refreshTokenSessionRepository
            .GetByIdAsync(refreshSessionId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.UserId != userId || !session.IsActive) {
            return Result.Failure<AuthenticationModel>(AuthenticationErrors.InvalidToken);
        }

        if (!VerifyRefreshToken(command.RefreshToken, session.RefreshTokenHash)) {
            return Result.Failure<AuthenticationModel>(AuthenticationErrors.InvalidToken);
        }

        Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
            .RecordAuthenticationAsync(
                userId,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(AuthenticationErrors.InvalidToken);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        IssuedAuthenticationTokens? tokens = await authenticationTokenService
            .RotateFromPrincipalAsync(
                principal,
                refreshSessionId.Value,
                session.RefreshTokenHash,
                rememberMe,
                cancellationToken)
            .ConfigureAwait(false);
        if (tokens is null) {
            return Result.Failure<AuthenticationModel>(AuthenticationErrors.InvalidToken);
        }
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }

    private bool VerifyRefreshToken(string refreshToken, string refreshTokenHash) =>
        SecurityTokenGenerator.IsFastStorageHash(refreshTokenHash)
            ? SecurityTokenGenerator.VerifyFastStorageHash(refreshToken, refreshTokenHash)
            : passwordHasher.Verify(SecurityTokenGenerator.NormalizeForSecureHashing(refreshToken), refreshTokenHash);
}
