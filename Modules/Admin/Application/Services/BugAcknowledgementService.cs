using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using System.Net;

namespace FoodDiary.Application.Admin.Services;

public sealed class BugAcknowledgementService(IBugAcknowledgementSource source, IEmailTemplateAdministrationReadService templates, IEmailTransport transport, IBugAcknowledgementReceipts receipts) {
    public async Task RunAsync(DateTimeOffset since, CancellationToken cancellationToken) {
        IReadOnlyList<EmailTemplateReadModel> available = await templates.GetTemplatesAsync(cancellationToken).ConfigureAwait(false);
        await foreach (BugAcknowledgementCandidate candidate in source.ReadAsync(since, cancellationToken).ConfigureAwait(false)) {
            EmailTemplateReadModel? template = available.FirstOrDefault(x => string.Equals(x.Key, "bug_report_received", StringComparison.Ordinal) && string.Equals(x.Locale, candidate.Locale, StringComparison.Ordinal))
                ?? available.FirstOrDefault(x => string.Equals(x.Key, "bug_report_received", StringComparison.Ordinal) && string.Equals(x.Locale, "en", StringComparison.Ordinal));
            // Deactivating the template disables acknowledgements; never replace it with a hard-coded fallback.
            if (template?.IsActive != true) {
                continue;
            }
            const string brand = "FoodDiary";
            await transport.SendAsync(new EmailMessage("no-reply@fooddiary.club", brand, [candidate.Recipient],
                template.Subject.Replace("{{brand}}", brand, StringComparison.OrdinalIgnoreCase),
                template.HtmlBody.Replace("{{brand}}", WebUtility.HtmlEncode(brand), StringComparison.OrdinalIgnoreCase),
                template.TextBody.Replace("{{brand}}", brand, StringComparison.OrdinalIgnoreCase),
                IdempotencyKey: candidate.IdempotencyKey, Purpose: "bug_report_received", ReplyTo: "bugs@fooddiary.club",
                InReplyTo: candidate.MessageId, AutoSubmitted: true, CorrelationId: candidate.InboxId.ToString()), cancellationToken).ConfigureAwait(false);
            await receipts.RecordAsync(candidate.InboxId, cancellationToken).ConfigureAwait(false);
        }
    }
}
