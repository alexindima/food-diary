using FoodDiary.MailInbox.Client.Models;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Export;
using MimeKit;

namespace FoodDiary.Infrastructure.Integrations.MailInbox;

internal sealed class BugAcknowledgementSource(IMailInboxExportClient export, IMailInboxClient inbox, IBugAcknowledgementReceipts receipts) : IBugAcknowledgementSource {
    public async IAsyncEnumerable<BugAcknowledgementCandidate> ReadAsync(DateTimeOffset since, [EnumeratorCancellation] CancellationToken cancellationToken) {
        DateTimeOffset? before = null;
        Guid? beforeId = null;
        while (true) {
            IReadOnlyList<MailInboxExportEntryResponse> page = await export.GetPageAsync("bugs@fooddiary.club", before, beforeId, cancellationToken).ConfigureAwait(false);
            if (page.Count == 0) {
                yield break;
            }
            foreach (MailInboxExportEntryResponse entry in page) {
                if (entry.ReceivedAtUtc < since) {
                    yield break;
                }
                if (!entry.ContentAvailable || await receipts.ContainsAsync(entry.Id, cancellationToken).ConfigureAwait(false)) {
                    continue;
                }
                InboundMailMessageDetailsResponse? details = await inbox.GetMessageAsync(entry.Id, cancellationToken).ConfigureAwait(false);
                if (details is null || string.IsNullOrWhiteSpace(details.EnvelopeFromAddress) || details.ContentPurgedAtUtc is not null) {
                    continue;
                }
                byte[]? bytes = await export.GetMimeAsync(entry.Id, "bugs@fooddiary.club", cancellationToken).ConfigureAwait(false);
                if (bytes is null) {
                    continue;
                }
                BugAcknowledgementCandidate? candidate = Parse(entry.Id, details.EnvelopeFromAddress, bytes);
                if (candidate is not null) {
                    yield return candidate;
                }
            }
            MailInboxExportEntryResponse last = page[^1];
            if (last.Id == beforeId) {
                throw new InvalidOperationException("MailInbox cursor did not advance.");
            }
            before = last.ReceivedAtUtc;
            beforeId = last.Id;
        }
    }

    internal static BugAcknowledgementCandidate? Parse(Guid id, string envelopeFrom, byte[] bytes) {
        try {
            using var stream = new MemoryStream(bytes, writable: false);
            using var message = MimeMessage.Load(stream);
            MailboxAddress[] senders = [.. message.From.Mailboxes];
            if (senders.Length != 1 || !MailboxAddress.TryParse(envelopeFrom, out MailboxAddress? envelope) ||
                !string.Equals(senders[0].Address, envelope.Address, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }
            string recipient = envelope.Address;
            string local = recipient.Split('@')[0];
            if (recipient.EndsWith("@fooddiary.club", StringComparison.OrdinalIgnoreCase) ||
                local.Contains("no-reply", StringComparison.OrdinalIgnoreCase) || local.Contains("noreply", StringComparison.OrdinalIgnoreCase) ||
                local.Equals("mailer-daemon", StringComparison.OrdinalIgnoreCase) || local.Equals("postmaster", StringComparison.OrdinalIgnoreCase) ||
                message.Headers.Contains("List-Id") || message.Headers.Contains("X-Auto-Response-Suppress") ||
                message.Headers.Contains("In-Reply-To") || message.References.Count > 0 ||
                message.Body is MultipartReport ||
                (message.Headers["Auto-Submitted"] is { } auto && !auto.Trim().Equals("no", StringComparison.OrdinalIgnoreCase)) ||
                (message.Headers["Precedence"] is { } precedence && new[] { "bulk", "list", "junk", "auto_reply" }.Contains(precedence.Trim(), StringComparer.OrdinalIgnoreCase))) {
                return null;
            }
            string identity = string.IsNullOrWhiteSpace(message.MessageId) ? id.ToString() : message.MessageId;
            string key = "bug-ack:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(recipient.ToLowerInvariant() + "\n" + identity)));
            string locale = (message.Subject + message.TextBody).Any(c => c is >= '\u0400' and <= '\u04ff') ? "ru" : "en";
            return new BugAcknowledgementCandidate(id, recipient, message.MessageId, key, locale);
        } catch (FormatException) {
            return null;
        }
    }
}
