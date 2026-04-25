using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminMailInboxReader {
    Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetMessagesAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<AdminMailInboxMessageDetailsModel?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken);
}
