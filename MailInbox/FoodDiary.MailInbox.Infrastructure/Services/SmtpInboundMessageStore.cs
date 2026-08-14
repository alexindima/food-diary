using System.Buffers;
using System.Text;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Application.Telemetry;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Diagnostics;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class SmtpInboundMessageStore(
    IInboundMailStore store,
    TimeProvider timeProvider,
    ILogger<SmtpInboundMessageStore> logger) : MessageStore {
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken) {
        long startedAt = Stopwatch.GetTimestamp();
        byte[] rawBytes = buffer.ToArray();
        using Activity? activity = MailInboxTelemetry.ActivitySource.StartActivity("MailInbox.Ingest");
        try {
            string rawMime = Encoding.UTF8.GetString(rawBytes);
            MimeMessage message = await ParseMessageAsync(rawBytes, cancellationToken).ConfigureAwait(false);
            string[] recipients = [.. transaction.To
                .Select(static mailbox => $"{mailbox.User}@{mailbox.Host}")
                .Where(static address => !string.IsNullOrWhiteSpace(address))];

            if (recipients.Length == 0) {
                recipients = [.. message.To.Mailboxes
                    .Select(static mailbox => mailbox.Address)
                    .Where(static address => !string.IsNullOrWhiteSpace(address))];
            }

            var inboundMessage = InboundMailMessage.Receive(
            message.MessageId,
            message.From.Mailboxes.FirstOrDefault()?.Address,
            recipients,
            message.Subject,
            message.TextBody,
            message.HtmlBody,
            rawMime,
            timeProvider.GetUtcNow());

            Guid id = await store.SaveAsync(inboundMessage, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
            "Received inbound email {MessageId}. StoredId={StoredId}; From={From}; RecipientCount={RecipientCount}",
            message.MessageId,
            id,
            message.From.Mailboxes.FirstOrDefault()?.Address,
            recipients.Length);

            MailInboxTelemetry.RecordIngestion("success", Stopwatch.GetElapsedTime(startedAt), rawBytes.LongLength);
            return SmtpResponse.Ok;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            MailInboxTelemetry.RecordIngestion("canceled", Stopwatch.GetElapsedTime(startedAt), rawBytes.LongLength);
            throw;
        } catch (Exception exception) {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            MailInboxTelemetry.RecordIngestion("failure", Stopwatch.GetElapsedTime(startedAt), rawBytes.LongLength);
            throw;
        }
    }

    private static async Task<MimeMessage> ParseMessageAsync(byte[] rawBytes, CancellationToken cancellationToken) {
        var stream = new MemoryStream(rawBytes);
        await using (stream.ConfigureAwait(false)) {
            return await MimeMessage.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }
}
