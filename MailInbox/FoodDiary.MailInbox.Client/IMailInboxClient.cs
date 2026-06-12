using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.MailInbox.Client;

public interface IMailInboxClient {
    Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetMessagesAsync(
        int? limit,
        CancellationToken cancellationToken);

    Task<InboundMailMessageDetailsResponse?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken);
}
