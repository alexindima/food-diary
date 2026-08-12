using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAuthenticationIdentityService {
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

    Task<Result<UserEmailVerificationDeliveryModel?>> IssueEmailVerificationAsync(
        UserId userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime issuedAtUtc,
        TimeSpan resendCooldown,
        CancellationToken cancellationToken = default);
}
