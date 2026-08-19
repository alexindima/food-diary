using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using System.Security.Cryptography;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserAuthenticationIdentityService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserGoogleIdentityRepository googleIdentityUserDirectoryService,
    IPasswordHasher passwordHasher)
    : IUserAuthenticationIdentityService {
    public async Task<Result<UserAuthenticationPrincipalModel>> AuthenticatePasswordAsync(
        string email,
        string password,
        DateTime authenticatedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(email, cancellationToken)
            .ConfigureAwait(false);
        if (user is null || !passwordHasher.Verify(password, user.Password)) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        UpgradePasswordHashIfNeeded(user, password);
        user.RecordAuthenticationActivity(authenticatedAtUtc);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAuthenticationPrincipal(user, authenticatedAtUtc));
    }

    public async Task<Result<UserAuthenticationPrincipalModel>> AuthenticateGoogleAsync(
        UserGoogleAuthenticationModel identity,
        DateTime authenticatedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await googleIdentityUserDirectoryService
            .GetByGoogleIdentityIncludingDeletedAsync(identity.Issuer, identity.Subject, cancellationToken)
            .ConfigureAwait(false);

        if (user is null) {
            User? emailOwner = await userLookupRepository
                .GetByEmailIncludingDeletedAsync(identity.Email, cancellationToken)
                .ConfigureAwait(false);
            if (emailOwner is not null) {
                return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.GoogleAccountLinkRequired);
            }

            user = CreateGoogleUser(identity);
            user.RecordAuthenticationActivity(authenticatedAtUtc);
            await userWriteRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
            return Result.Success(ToAuthenticationPrincipal(user, authenticatedAtUtc));
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        ApplyGoogleProfile(user, identity);
        user.RecordAuthenticationActivity(authenticatedAtUtc);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAuthenticationPrincipal(user, authenticatedAtUtc));
    }

    public async Task<Result<UserAuthenticationPrincipalModel>> AuthenticateTelegramAsync(
        long telegramUserId,
        DateTime authenticatedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByTelegramUserIdAsync(telegramUserId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.TelegramNotLinked);
        }

        user.RecordAuthenticationActivity(authenticatedAtUtc);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAuthenticationPrincipal(user, authenticatedAtUtc));
    }

    public async Task<Result<UserAuthenticationPrincipalModel>> CompletePasswordResetAsync(
        UserId userId,
        string token,
        string newPassword,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.User.NotFound(userId));
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
            !user.PasswordResetTokenExpiresAtUtc.HasValue ||
            user.PasswordResetTokenExpiresAtUtc.Value <= completedAtUtc ||
            !passwordHasher.Verify(token, user.PasswordResetTokenHash)) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidToken);
        }

        user.CompletePasswordReset(passwordHasher.Hash(newPassword));
        user.RecordAuthenticationActivity(completedAtUtc);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAuthenticationPrincipal(user, completedAtUtc));
    }

    public async Task<UserPasswordResetIssueModel> IssuePasswordResetAsync(
        string email,
        string token,
        DateTime expiresAtUtc,
        DateTime issuedAtUtc,
        TimeSpan resendCooldown,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(email, cancellationToken)
            .ConfigureAwait(false);
        if (user is not { IsActive: true, DeletedAt: null }) {
            return new UserPasswordResetIssueModel(UserPasswordResetIssueStatus.NotEligible);
        }

        if (user.PasswordResetSentAtUtc.HasValue &&
            issuedAtUtc - user.PasswordResetSentAtUtc.Value < resendCooldown) {
            return new UserPasswordResetIssueModel(UserPasswordResetIssueStatus.Throttled);
        }

        user.SetPasswordResetToken(new UserTokenIssue(
            TokenHash: passwordHasher.Hash(token),
            ExpiresAtUtc: expiresAtUtc,
            IssuedAtUtc: issuedAtUtc));
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return new UserPasswordResetIssueModel(
            UserPasswordResetIssueStatus.Issued,
            new UserPasswordResetDeliveryModel(user.Id.Value, user.Email, user.Language));
    }

    public async Task<Result<bool>> VerifyEmailAsync(
        UserId userId,
        string token,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<bool>(Errors.User.NotFound(userId));
        }

        if (user.IsEmailConfirmed) {
            return Result.Success(value: false);
        }

        if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash) ||
            !user.EmailConfirmationTokenExpiresAtUtc.HasValue ||
            user.EmailConfirmationTokenExpiresAtUtc.Value <= verifiedAtUtc ||
            !passwordHasher.Verify(token, user.EmailConfirmationTokenHash)) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        user.CompleteEmailVerification();
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(value: true);
    }

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

    public async Task<Result<UserAuthenticationPrincipalModel>> RecordAuthenticationAsync(
        UserId userId,
        DateTime authenticatedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        user.RecordAuthenticationActivity(authenticatedAtUtc);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAuthenticationPrincipal(user, authenticatedAtUtc));
    }

    public async Task<Result<UserAuthenticationPrincipalModel>> GetAuthenticationPrincipalAsync(
        UserId userId,
        DateTime evaluatedAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.User.NotFound());
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        return Result.Success(ToAuthenticationPrincipal(user, evaluatedAtUtc));
    }

    public async Task<Result<UserAuthenticationPrincipalModel>> RestoreAccountAsync(
        string email,
        string password,
        DateTime restoredAtUtc,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(email, cancellationToken)
            .ConfigureAwait(false);
        if (user is null || !passwordHasher.Verify(password, user.Password)) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.AccountNotDeleted);
        }

        user.Restore(restoredAtUtc);
        UpgradePasswordHashIfNeeded(user, password);
        user.RecordAuthenticationActivity(restoredAtUtc);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAuthenticationPrincipal(user, restoredAtUtc));
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

    private void UpgradePasswordHashIfNeeded(User user, string password) {
        if (passwordHasher.NeedsRehash(user.Password)) {
            bool mustChangePassword = user.MustChangePassword;
            user.UpdatePassword(passwordHasher.Hash(password));
            if (mustChangePassword) {
                user.RequirePasswordChange();
            }
        }
    }

    internal static UserAuthenticationPrincipalModel ToAuthenticationPrincipal(User user, DateTime authenticatedAtUtc) {
        var roles = user.GetRoleNames().ToList();
        bool hasActivePremiumTrial = user.HasActivePremiumTrial(authenticatedAtUtc);
        if (hasActivePremiumTrial && !roles.Contains(RoleNames.Premium, StringComparer.Ordinal)) {
            roles.Add(RoleNames.Premium);
        }

        DateTime? accessTokenCapUtc = user.HasRole(RoleNames.Premium) || !hasActivePremiumTrial
            ? null
            : user.PremiumTrialEndsAtUtc;
        return new UserAuthenticationPrincipalModel(
            user.Id,
            user.Email,
            roles,
            accessTokenCapUtc,
            user.ToModel());
    }

    private User CreateGoogleUser(UserGoogleAuthenticationModel identity) {
        string placeholderPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var user = User.Create(identity.Email, passwordHasher.Hash(placeholderPassword), hasPassword: false);
        user.LinkGoogleIdentity(identity.Issuer, identity.Subject);
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        ApplyGoogleProfile(user, identity);
        return user;
    }

    private static void ApplyGoogleProfile(User user, UserGoogleAuthenticationModel identity) {
        if (!user.IsEmailConfirmed) {
            user.SetEmailConfirmed(isConfirmed: true);
        }

        if (!string.IsNullOrWhiteSpace(identity.Locale)) {
            string normalizedLanguage = LanguageCode.FromPreferred(identity.Locale).Value;
            if (!string.Equals(user.Language, normalizedLanguage, StringComparison.Ordinal)) {
                user.SetLanguage(normalizedLanguage);
            }
        }

        user.UpdatePersonalInfo(identity.FirstName, identity.LastName);
    }
}
