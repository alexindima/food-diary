using System.Buffers;
using System.Text;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Domain.Messages;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class SmtpInboundMessageStore(
    IInboundMailStore store,
    ILogger<SmtpInboundMessageStore> logger) : MessageStore {
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken) {
        byte[] rawBytes = buffer.ToArray();
        string rawMime = Encoding.UTF8.GetString(rawBytes);
        MimeMessage message = await ParseMessageAsync(rawBytes, cancellationToken).ConfigureAwait(false);
        string[] recipients = message.To.Mailboxes
            .Select(static mailbox => mailbox.Address)
            .Where(static address => !string.IsNullOrWhiteSpace(address))
            .ToArray();

        if (recipients.Length == 0) {
            recipients = transaction.To
                .Select(static mailbox => $"{mailbox.User}@{mailbox.Host}")
                .ToArray();
        }

        var inboundMessage = InboundMailMessage.Receive(
            message.MessageId,
            message.From.Mailboxes.FirstOrDefault()?.Address,
            recipients,
            message.Subject,
            message.TextBody,
            message.HtmlBody,
            rawMime,
            DateTimeOffset.UtcNow);

        Guid id = await store.SaveAsync(inboundMessage, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Received inbound email {MessageId}. StoredId={StoredId}; From={From}; RecipientCount={RecipientCount}",
            message.MessageId,
            id,
            message.From.Mailboxes.FirstOrDefault()?.Address,
            recipients.Length);

        return SmtpResponse.Ok;
    }

    private static async Task<MimeMessage> ParseMessageAsync(byte[] rawBytes, CancellationToken cancellationToken) {
        var stream = new MemoryStream(rawBytes);
        await using (stream.ConfigureAwait(false)) {
            return await MimeMessage.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }
}
