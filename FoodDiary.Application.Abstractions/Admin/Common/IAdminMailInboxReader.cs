using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminMailInboxReader {
    Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetMessagesAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<AdminMailInboxMessageDetailsModel?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken);
}
