using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface ITelegramDiaryContentTransfer {
    int Order { get; }
    Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken);
}
