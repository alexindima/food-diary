using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService,
    IAuditLogger auditLogger)
    : ICommandHandler<ConfirmPasswordResetCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(ConfirmPasswordResetCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure<AuthenticationModel>(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

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
        user.CompletePasswordReset(hashedPassword);

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        auditLogger.Log("auth.password-reset.confirm", userId, "User", userId.Value.ToString());

        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
