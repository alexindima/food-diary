using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserTelegramDiaryTransfer {
    Task<TelegramDiaryTarget?> FindTargetAsync(string verifiedEmail, CancellationToken cancellationToken);
    Task<Result<TelegramDiaryPreview>> TransferAsync(
        TelegramDiaryProof proof, bool? useTelegramGoals, CancellationToken cancellationToken);
}
