using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface ITelegramDiaryTransferReader {
    Task<Result<TelegramDiaryCounts>> ReadAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken);
}
