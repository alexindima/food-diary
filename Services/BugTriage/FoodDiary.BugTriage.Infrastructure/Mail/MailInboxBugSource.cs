using System.Runtime.CompilerServices;
using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Options;
using FoodDiary.MailInbox.Client.Export;
using FoodDiary.MailInbox.Client.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FoodDiary.BugTriage.Infrastructure.Mail;

public sealed class MailInboxBugSource(IMailInboxExportClient client, IBugReportStore store,
    IOptions<BugTriageOptions> options) : IBugMailSource {
    public async IAsyncEnumerable<ImportedReport> ReadNewAsync([EnumeratorCancellation] CancellationToken cancellationToken) {
        // Revisit retained mail each scan: late commits and SMTP retries cannot be lost behind a high-water mark.
        DateTimeOffset? before = null;
        Guid? beforeId = null;
        int imported = 0;
        while (true) {
            IReadOnlyList<MailInboxExportEntryResponse> page = await client.GetPageAsync(options.Value.Recipient, before, beforeId, cancellationToken).ConfigureAwait(false);
            if (page.Count == 0) {
                yield break;
            }
            foreach (MailInboxExportEntryResponse entry in page) {
                cancellationToken.ThrowIfCancellationRequested();
                if (await store.ContainsAsync(entry.Id, cancellationToken).ConfigureAwait(false)) {
                    continue;
                }
                if (imported >= options.Value.MaxImportsPerPoll) {
                    yield break;
                }
                imported++;
                byte[]? mime = entry.ContentAvailable
                    ? await client.GetMimeAsync(entry.Id, options.Value.Recipient, cancellationToken).ConfigureAwait(false)
                    : null;
                if (mime is null) {
                    yield return new ImportedReport(entry.Id, entry.ReceivedAtUtc, "Content unavailable", string.Empty, RawMime: null);
                    continue;
                }
                ImportedReport report;
                try {
                    var stream = new MemoryStream(mime, writable: false);
                    await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
                    using MimeMessage message = await MimeMessage.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
                    string body = message.TextBody ?? message.HtmlBody ?? string.Empty;
                    report = new ImportedReport(entry.Id, entry.ReceivedAtUtc, Bound(message.Subject ?? "(no subject)", 1000), Bound(body, 100_000), mime);
                } catch (FormatException) {
                    report = new ImportedReport(entry.Id, entry.ReceivedAtUtc, "Unparseable email", string.Empty, mime);
                }
                yield return report;
            }
            MailInboxExportEntryResponse last = page[^1];
            if (last.Id == beforeId) {
                throw new InvalidOperationException("MailInbox export cursor did not advance.");
            }
            before = last.ReceivedAtUtc;
            beforeId = last.Id;
        }
    }

    private static string Bound(string value, int maxLength) => value.Length <= maxLength
        ? value : value[..(char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength)];
}
