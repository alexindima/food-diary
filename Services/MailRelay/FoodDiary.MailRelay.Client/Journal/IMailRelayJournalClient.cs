using FoodDiary.MailRelay.Client.Models;

namespace FoodDiary.MailRelay.Client.Journal;

public interface IMailRelayJournalClient {
    Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null);
}
