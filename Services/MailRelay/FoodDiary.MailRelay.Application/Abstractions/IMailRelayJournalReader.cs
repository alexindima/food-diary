namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelayJournalReader {
    Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null);
}
