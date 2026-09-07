using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.MailInbox.Client;

public interface IMailInboxClient {
    Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetMessagesAsync(
        int? limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetFilteredMessagesAsync(
        int? limit, string? recipient, string? category, bool? unread, CancellationToken cancellationToken) =>
        recipient is null && category is null && unread is null
            ? GetMessagesAsync(limit, cancellationToken)
            : throw new NotSupportedException("Filtered message queries are not implemented.");

    Task<InboundMailMessageDetailsResponse?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> MarkMessageReadAsync(
        Guid id,
        CancellationToken cancellationToken);
}
