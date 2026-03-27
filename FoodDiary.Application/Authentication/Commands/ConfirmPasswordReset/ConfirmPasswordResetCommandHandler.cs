using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<ConfirmPasswordResetCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(ConfirmPasswordResetCommand command, CancellationToken cancellationToken) {
        var userId = new UserId(command.UserId);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.User.NotFound(userId));
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
            !user.PasswordResetTokenExpiresAtUtc.HasValue ||
            user.PasswordResetTokenExpiresAtUtc.Value < dateTimeProvider.UtcNow) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var isValid = passwordHasher.Verify(command.Token, user.PasswordResetTokenHash);
        if (!isValid) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidToken);
        }

        var hashedPassword = passwordHasher.Hash(command.NewPassword);
        user.UpdatePassword(hashedPassword);
        user.ClearPasswordResetToken();

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
