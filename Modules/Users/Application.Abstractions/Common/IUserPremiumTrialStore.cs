using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserPremiumTrialStore {
    Task LockAccountAsync(UserId userId, CancellationToken cancellationToken);
    Task<bool> HasRedeemedTelegramAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<bool> TryRedeemTelegramAsync(long telegramUserId, CancellationToken cancellationToken);
}
