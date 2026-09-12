using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserTelegramAccountService {
    Task<Result<bool>> IsRegisteredAsync(long telegramUserId, CancellationToken cancellationToken = default);

    Task<Result<UserAuthenticationPrincipalModel>> RegisterAsync(
        UserTelegramRegistrationModel registration,
        CancellationToken cancellationToken = default);

    Task<Result> UnlinkAsync(UserId userId, long telegramUserId, long securityVersion, CancellationToken cancellationToken = default);

    Task<Result> BindOidcIdentityAsync(UserId userId, long telegramUserId, string issuer, string subject,
        CancellationToken cancellationToken = default);

    Task<Result> AddVerifiedEmailAsync(UserId userId, string email, long securityVersion, CancellationToken cancellationToken = default);
}
