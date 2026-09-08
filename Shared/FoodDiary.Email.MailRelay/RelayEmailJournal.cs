using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.MailRelay.Client.Journal;

namespace FoodDiary.Integrations.Services;

internal sealed class RelayEmailJournal(IMailRelayJournalClient client) : IOutgoingEmailJournal {
    public async Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null) {
        FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalPage result = await client.GetPageAsync(page, limit, purpose, status, recipient, cancellationToken, fromUtc, toUtc, id, correlationId).ConfigureAwait(false);
        return new OutgoingEmailJournalPage(result.Items.Select(x => new OutgoingEmailJournalEntry(x.Id, x.Status, x.Purpose, x.FromAddress, x.To,
            x.Subject, x.CreatedAtUtc, x.SentAtUtc, x.AttemptCount, x.MaxAttempts, x.CorrelationId, x.TextBody, x.ContentHidden, x.ReplyTo, x.InReplyTo)).ToList(), result.TotalItems, result.StatusCounts);
    }
}
