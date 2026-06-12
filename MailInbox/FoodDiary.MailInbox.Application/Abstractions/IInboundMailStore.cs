using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;

namespace FoodDiary.MailInbox.Application.Abstractions;

public interface IInboundMailStore {
    Task<Guid> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken);

    Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken);
}
