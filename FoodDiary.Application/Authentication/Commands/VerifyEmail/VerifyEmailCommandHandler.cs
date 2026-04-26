using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IEmailVerificationNotifier emailVerificationNotifier)
    : ICommandHandler<VerifyEmailCommand, Result> {
    private readonly IEmailVerificationNotifier _emailVerificationNotifier = emailVerificationNotifier;
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var userId = new UserId(command.UserId);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure(Errors.User.NotFound(userId));
        }

        if (user.IsEmailConfirmed) {
            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash) ||
            !user.EmailConfirmationTokenExpiresAtUtc.HasValue ||
            user.EmailConfirmationTokenExpiresAtUtc.Value < dateTimeProvider.UtcNow) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var isValid = passwordHasher.Verify(command.Token, user.EmailConfirmationTokenHash);
        if (!isValid) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        user.CompleteEmailVerification();
        await userRepository.UpdateAsync(user, cancellationToken);

        try {
            await _emailVerificationNotifier.NotifyEmailVerifiedAsync(user.Id.Value, cancellationToken);
        } catch {
            // Notification failures shouldn't block verification.
        }

        return Result.Success();
    }
}
