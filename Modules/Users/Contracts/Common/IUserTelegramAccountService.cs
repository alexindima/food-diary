using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

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
