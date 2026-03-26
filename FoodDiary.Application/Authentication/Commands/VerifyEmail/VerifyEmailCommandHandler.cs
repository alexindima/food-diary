using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IEmailVerificationNotifier emailVerificationNotifier)
    : ICommandHandler<VerifyEmailCommand, Result<bool>> {
    private readonly IEmailVerificationNotifier _emailVerificationNotifier = emailVerificationNotifier;
    public async Task<Result<bool>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken) {
        var userId = new UserId(command.UserId);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<bool>(Errors.User.NotFound(userId));
        }

        if (user.IsEmailConfirmed) {
            return Result.Success(true);
        }

        if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash) ||
            !user.EmailConfirmationTokenExpiresAtUtc.HasValue ||
            user.EmailConfirmationTokenExpiresAtUtc.Value < dateTimeProvider.UtcNow) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var isValid = passwordHasher.Verify(command.Token, user.EmailConfirmationTokenHash);
        if (!isValid) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        user.ConfirmEmail();
        await userRepository.UpdateAsync(user, cancellationToken);

        try {
            await _emailVerificationNotifier.NotifyEmailVerifiedAsync(user.Id.Value, cancellationToken);
        } catch {
            // Notification failures shouldn't block verification.
        }

        return Result.Success(true);
    }
}
