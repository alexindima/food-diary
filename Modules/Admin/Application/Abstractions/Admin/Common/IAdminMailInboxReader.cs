using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminMailInboxReader {
    Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetMessagesAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetFilteredMessagesAsync(
        int limit, string? recipient, string? category, bool? unread, CancellationToken cancellationToken) =>
        recipient is null && category is null && unread is null
            ? GetMessagesAsync(limit, cancellationToken)
            : throw new NotSupportedException("Filtered message queries are not implemented.");

    Task<AdminMailInboxMessageDetailsModel?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> MarkMessageReadAsync(
        Guid id,
        CancellationToken cancellationToken);
}
