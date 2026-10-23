using ClientJournalEntry = FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalEntry;
using ClientJournalPage = FoodDiary.MailRelay.Client.Models.OutgoingEmailJournalPage;
using FoodDiary.MailRelay.Application.Emails.Queries.GetOutgoingEmailJournal;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Mappings;

public static class OutgoingEmailJournalHttpMappings {
    public static GetOutgoingEmailJournalQuery ToQuery(this GetOutgoingEmailJournalHttpQuery query) =>
        new(query.Page, query.Limit, query.Purpose, query.Status, query.Recipient, query.FromUtc, query.ToUtc, query.Id, query.CorrelationId);

    public static ClientJournalPage ToJournalHttpResponse(this OutgoingEmailJournalPage page) =>
        new(page.Items.Select(x => new ClientJournalEntry(x.Id, x.Status, x.Purpose, x.FromAddress, x.To,
            x.Subject, x.CreatedAtUtc, x.SentAtUtc, x.AttemptCount, x.MaxAttempts, x.CorrelationId, x.TextBody, x.ContentHidden, x.ReplyTo, x.InReplyTo)).ToList(), page.TotalItems);
}
