using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<ConfirmPasswordResetCommand, Result<AuthenticationResponse>> {
    public async Task<Result<AuthenticationResponse>> Handle(ConfirmPasswordResetCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user is null) {
            return Result.Failure<AuthenticationResponse>(Errors.User.NotFound(command.UserId.Value));
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
            !user.PasswordResetTokenExpiresAtUtc.HasValue ||
            user.PasswordResetTokenExpiresAtUtc.Value < dateTimeProvider.UtcNow) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidToken);
        }

        var isValid = passwordHasher.Verify(command.Token, user.PasswordResetTokenHash);
        if (!isValid) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidToken);
        }

        var hashedPassword = passwordHasher.Hash(command.NewPassword);
        user.UpdatePassword(hashedPassword);
        user.ClearPasswordResetToken();

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        var userResponse = user.ToResponse();
        return Result.Success(new AuthenticationResponse(tokens.AccessToken, tokens.RefreshToken, userResponse));
    }
}
