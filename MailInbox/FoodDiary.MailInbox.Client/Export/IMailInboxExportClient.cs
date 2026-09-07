using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.MailInbox.Client.Export;

public interface IMailInboxExportClient {
    Task<IReadOnlyList<MailInboxExportEntryResponse>> GetPageAsync(string recipient,
        DateTimeOffset? beforeReceivedAtUtc, Guid? beforeId, CancellationToken cancellationToken);
    Task<byte[]?> GetMimeAsync(Guid id, string recipient, CancellationToken cancellationToken);
}
