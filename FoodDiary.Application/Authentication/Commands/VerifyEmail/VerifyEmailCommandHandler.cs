using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider dateTimeProvider,
    IPostCommitActionQueue postCommitActionQueue,
    IEmailVerificationNotifier emailVerificationNotifier)
    : ICommandHandler<VerifyEmailCommand, Result> {
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var userId = new UserId(command.UserId);
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure(Errors.User.NotFound(userId));
        }

        if (user.IsEmailConfirmed) {
            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash) ||
            !user.EmailConfirmationTokenExpiresAtUtc.HasValue ||
            user.EmailConfirmationTokenExpiresAtUtc.Value < dateTimeProvider.GetUtcNow().UtcDateTime) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        bool isValid = passwordHasher.Verify(command.Token, user.EmailConfirmationTokenHash);
        if (!isValid) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        user.CompleteEmailVerification();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        postCommitActionQueue.Enqueue(async ct => {
            try {
                await emailVerificationNotifier.NotifyEmailVerifiedAsync(user.Id.Value, ct).ConfigureAwait(false);
            } catch {
                // Notification failures shouldn't block verification.
            }
        });

        return Result.Success();
    }
}
