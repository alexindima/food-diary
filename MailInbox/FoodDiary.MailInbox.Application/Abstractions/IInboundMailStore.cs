using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;

namespace FoodDiary.MailInbox.Application.Abstractions;

public interface IInboundMailStore {
    Task<InboundMailSaveResult> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken);

    Task<InboundMailSaveResult> SaveAsync(
        InboundMailMessage message,
        InboundMailAdmission admission,
        CancellationToken cancellationToken) =>
        SaveAsync(message, cancellationToken);

    Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken);

    Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken);

    Task<InboundMailRetentionResult> PurgeExpiredAsync(
        DateTimeOffset contentCutoffUtc,
        DateTimeOffset metadataCutoffUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
