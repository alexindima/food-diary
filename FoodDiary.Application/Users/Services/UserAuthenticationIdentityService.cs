using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserAuthenticationIdentityService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IGoogleIdentityUserDirectoryService googleIdentityUserDirectoryService)
    : IUserAuthenticationIdentityService {
    public async Task<Result<UserModel>> LinkGoogleAsync(
        UserId userId,
        string email,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserModel>(userResult.Error);
        }

        User user = userResult.Value;
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<UserModel>(Errors.Authentication.GoogleAccountEmailMismatch);
        }

        bool hasGoogleIdentity =
            !string.IsNullOrWhiteSpace(user.GoogleIssuer) &&
            !string.IsNullOrWhiteSpace(user.GoogleSubject);
        if (hasGoogleIdentity) {
            bool isSameIdentity =
                string.Equals(user.GoogleIssuer, issuer, StringComparison.Ordinal) &&
                string.Equals(user.GoogleSubject, subject, StringComparison.Ordinal);
            return isSameIdentity
                ? Result.Success(user.ToModel())
                : Result.Failure<UserModel>(Errors.Authentication.GoogleIdentityDifferent);
        }

        User? identityOwner = await googleIdentityUserDirectoryService
            .GetByGoogleIdentityIncludingDeletedAsync(issuer, subject, cancellationToken)
            .ConfigureAwait(false);
        if (identityOwner is not null && identityOwner.Id != user.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.GoogleIdentityAlreadyLinked);
        }

        user.LinkGoogleIdentity(issuer, subject);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToModel());
    }

    public async Task<Result<UserModel>> LinkTelegramAsync(
        UserId userId,
        long telegramUserId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserModel>(userResult.Error);
        }

        User user = userResult.Value;
        if (user.TelegramUserId == telegramUserId) {
            return Result.Success(user.ToModel());
        }

        User? identityOwner = await userLookupRepository
            .GetByTelegramUserIdIncludingDeletedAsync(telegramUserId, cancellationToken)
            .ConfigureAwait(false);
        if (identityOwner is not null && identityOwner.Id != user.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.TelegramAlreadyLinked);
        }

        user.LinkTelegram(telegramUserId);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToModel());
    }

    public async Task<Result<UserEmailVerificationDeliveryModel?>> IssueEmailVerificationAsync(
        UserId userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime issuedAtUtc,
        TimeSpan resendCooldown,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserEmailVerificationDeliveryModel?>(userResult.Error);
        }

        User user = userResult.Value;
        if (user.IsEmailConfirmed) {
            return Result.Success<UserEmailVerificationDeliveryModel?>(value: null);
        }

        if (user.EmailConfirmationSentAtUtc.HasValue && issuedAtUtc - user.EmailConfirmationSentAtUtc.Value < resendCooldown) {
            return Result.Failure<UserEmailVerificationDeliveryModel?>(
                Errors.Validation.Invalid(
                    "EmailVerification",
                    "Verification email was sent recently. Please wait before requesting a new one."));
        }

        user.SetEmailConfirmationToken(new UserTokenIssue(
            TokenHash: tokenHash,
            ExpiresAtUtc: expiresAtUtc,
            IssuedAtUtc: issuedAtUtc));
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success<UserEmailVerificationDeliveryModel?>(new UserEmailVerificationDeliveryModel(
            user.Id.Value,
            user.Email,
            user.Language));
    }

    private async Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return error is not null ? Result.Failure<User>(error) : Result.Success(user!);
    }
}
