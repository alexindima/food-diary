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
        var rawBytes = buffer.ToArray();
        var rawMime = Encoding.UTF8.GetString(rawBytes);
        var message = await ParseMessageAsync(rawBytes, cancellationToken);
        var recipients = message.To.Mailboxes
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

        var id = await store.SaveAsync(inboundMessage, cancellationToken);

        logger.LogInformation(
            "Received inbound email {MessageId}. StoredId={StoredId}; From={From}; RecipientCount={RecipientCount}",
            message.MessageId,
            id,
            message.From.Mailboxes.FirstOrDefault()?.Address,
            recipients.Length);

        return SmtpResponse.Ok;
    }

    private static async Task<MimeMessage> ParseMessageAsync(byte[] rawBytes, CancellationToken cancellationToken) {
        await using var stream = new MemoryStream(rawBytes);
        return await MimeMessage.LoadAsync(stream, cancellationToken);
    }
}
