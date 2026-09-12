using System.Security.Cryptography;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserTelegramAccountService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserSessionRevocationService sessionRevocationService,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IUserTelegramAccountService {
    public async Task<Result<bool>> IsRegisteredAsync(long telegramUserId, CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByTelegramUserIdIncludingDeletedAsync(telegramUserId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Success(value: false);
        }
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return error is null ? Result.Success(value: true) : Result.Failure<bool>(error);
    }

    public async Task<Result<UserAuthenticationPrincipalModel>> RegisterAsync(
        UserTelegramRegistrationModel registration,
        CancellationToken cancellationToken = default) {
        User? existing = await userLookupRepository
            .GetByTelegramUserIdIncludingDeletedAsync(registration.TelegramUserId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.TelegramAlreadyLinked);
        }

        string inaccessiblePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var user = User.CreateTelegram(registration.TelegramUserId, passwordHasher.Hash(inaccessiblePassword));
        if (registration.OidcIssuer is not null && registration.OidcSubject is not null) {
            user.BindTelegramOidcIdentity(registration.OidcIssuer, registration.OidcSubject);
        }
        user.SetLanguage(LanguageCode.FromPreferred(registration.Language).Value);
        user.SetTimeZone(registration.TimeZoneId);
        user.UpdatePersonalInfo(firstName: registration.FirstName, lastName: registration.LastName);
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.RecordAuthenticationActivity(registration.RegisteredAtUtc);
        await userWriteRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, registration.RegisteredAtUtc));
    }

    public async Task<Result> BindOidcIdentityAsync(UserId userId, long telegramUserId, string issuer, string subject,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (error is not null) {
            return Result.Failure(error);
        }
        if (user!.TelegramUserId != telegramUserId ||
            (user.TelegramOidcIssuer is not null && !string.Equals(user.TelegramOidcIssuer, issuer, StringComparison.Ordinal)) ||
            (user.TelegramOidcSubject is not null && !string.Equals(user.TelegramOidcSubject, subject, StringComparison.Ordinal))) {
            return Result.Failure(UserErrors.TelegramAlreadyLinked);
        }
        user.BindTelegramOidcIdentity(issuer, subject);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> UnlinkAsync(UserId userId, long telegramUserId, long securityVersion, CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (error is not null) {
            return Result.Failure(error);
        }
        if (!user!.TelegramUserId.HasValue) {
            return Result.Success();
        }
        if (user.TelegramUserId != telegramUserId || user.SecurityVersion != securityVersion) {
            return Result.Failure(new Error("Authentication.TelegramProofStale", "Confirm the current Telegram account again.", ErrorKind.Conflict));
        }
        bool hasPasswordLogin = user.HasPassword && user.Email is not null && user.IsEmailConfirmed;
        bool hasGoogleLogin = user.GoogleIssuer is not null && user.GoogleSubject is not null;
        if (!hasPasswordLogin && !hasGoogleLogin) {
            return Result.Failure(UserErrors.LastSignInMethod);
        }

        user.UnlinkTelegram();
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await sessionRevocationService.RevokeAllAsync(
            userId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> AddVerifiedEmailAsync(
        UserId userId, string email, long securityVersion, CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (error is not null) {
            return Result.Failure(error);
        }
        if (user!.SecurityVersion != securityVersion || !user.TelegramUserId.HasValue) {
            return Result.Failure(new Error("Authentication.TelegramProofStale", "Confirm the current Telegram account again.", ErrorKind.Conflict));
        }
        if (user.Email is not null) {
            return Result.Failure(UserErrors.EmailAlreadyExists);
        }
        User? owner = await userLookupRepository.GetByEmailIncludingDeletedAsync(email, cancellationToken).ConfigureAwait(false);
        if (owner is not null) {
            return Result.Failure(UserErrors.EmailAlreadyExists);
        }

        user.AddVerifiedEmail(email);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await sessionRevocationService.RevokeAllAsync(
            userId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
