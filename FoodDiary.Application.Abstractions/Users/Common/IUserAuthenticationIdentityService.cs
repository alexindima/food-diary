using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAuthenticationIdentityService {
    Task<Result<UserAuthenticationPrincipalModel>> AuthenticatePasswordAsync(
        string email,
        string password,
        DateTime authenticatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<Result<UserAuthenticationPrincipalModel>> CompletePasswordResetAsync(
        UserId userId,
        string token,
        string newPassword,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default);

    Task<UserPasswordResetIssueModel> IssuePasswordResetAsync(
        string email,
        string token,
        DateTime expiresAtUtc,
        DateTime issuedAtUtc,
        TimeSpan resendCooldown,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> VerifyEmailAsync(
        UserId userId,
        string token,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken = default);

    Task<Result<UserModel>> LinkGoogleAsync(
        UserId userId,
        string email,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);

    Task<Result<UserModel>> LinkTelegramAsync(
        UserId userId,
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<Result<UserAuthenticationPrincipalModel>> RecordAuthenticationAsync(
        UserId userId,
        DateTime authenticatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<Result<UserAuthenticationPrincipalModel>> RestoreAccountAsync(
        string email,
        string password,
        DateTime restoredAtUtc,
        CancellationToken cancellationToken = default);

    Task<Result<UserEmailVerificationDeliveryModel?>> IssueEmailVerificationAsync(
        UserId userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime issuedAtUtc,
        TimeSpan resendCooldown,
        CancellationToken cancellationToken = default);
}
