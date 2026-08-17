using System.Buffers;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Application.Telemetry;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Diagnostics;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class SmtpInboundMessageStore(
    IInboundMailStore store,
    IOptions<MailInboxSmtpOptions> options,
    TimeProvider timeProvider,
    ILogger<SmtpInboundMessageStore> logger) : MessageStore, IDisposable {
    private readonly MailInboxSmtpOptions _options = options.Value;
    private readonly SemaphoreSlim _processingSlots = new(
        options.Value.MaxConcurrentMessageProcessing,
        options.Value.MaxConcurrentMessageProcessing);

    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken) {
        long startedAt = Stopwatch.GetTimestamp();
        long messageSize = buffer.Length;
        using Activity? activity = MailInboxTelemetry.ActivitySource.StartActivity("MailInbox.Ingest");
        bool entered = await _processingSlots
            .WaitAsync(_options.ProcessingQueueTimeout, cancellationToken)
            .ConfigureAwait(false);
        if (!entered) {
            MailInboxTelemetry.RecordIngestion("overloaded", Stopwatch.GetElapsedTime(startedAt), messageSize);
            return new SmtpResponse(SmtpReplyCode.Overloaded, "Mail processing capacity is temporarily exhausted.");
        }

        try {
            if (messageSize > _options.MaxMessageSizeBytes) {
                MailInboxTelemetry.RecordIngestion("message_too_large", Stopwatch.GetElapsedTime(startedAt), messageSize);
                return SmtpResponse.SizeLimitExceeded;
            }

            byte[] rawBytes = buffer.ToArray();
            MimeMessage message = await ParseMessageAsync(rawBytes, cancellationToken).ConfigureAwait(false);
            if (message.BodyParts.Take(_options.MaxMimeParts + 1).Count() > _options.MaxMimeParts) {
                MailInboxTelemetry.RecordIngestion("mime_part_limit", Stopwatch.GetElapsedTime(startedAt), messageSize);
                return new SmtpResponse(SmtpReplyCode.TransactionFailed, "MIME part limit exceeded.");
            }

            string[] recipients = [.. transaction.To
                .Select(static mailbox => $"{mailbox.User}@{mailbox.Host}")
                .Where(static address => !string.IsNullOrWhiteSpace(address))];

            if (recipients.Length == 0) {
                recipients = [.. message.To.Mailboxes
                    .Select(static mailbox => mailbox.Address)
                    .Where(static address => !string.IsNullOrWhiteSpace(address))];
            }

            if (recipients.Length == 0 || recipients.Length > _options.MaxRecipientsPerMessage) {
                MailInboxTelemetry.RecordIngestion("recipient_limit", Stopwatch.GetElapsedTime(startedAt), messageSize);
                return SmtpResponse.NoValidRecipientsGiven;
            }

            var inboundMessage = InboundMailMessage.Receive(
                message.MessageId,
                message.From.Mailboxes.FirstOrDefault()?.Address,
                recipients,
                message.Subject,
                Truncate(message.TextBody, _options.MaxExtractedBodyCharacters),
                Truncate(message.HtmlBody, _options.MaxExtractedBodyCharacters),
                rawBytes,
                timeProvider.GetUtcNow());

            InboundMailSaveResult saveResult = await store.SaveAsync(inboundMessage, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Inbound email accepted. StoredId={StoredId}; RecipientCount={RecipientCount}; Duplicate={Duplicate}",
                saveResult.Id,
                recipients.Length,
                saveResult.WasDuplicate);

            MailInboxTelemetry.RecordIngestion(
                saveResult.WasDuplicate ? "duplicate" : "success",
                Stopwatch.GetElapsedTime(startedAt),
                messageSize);
            return SmtpResponse.Ok;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            MailInboxTelemetry.RecordIngestion("canceled", Stopwatch.GetElapsedTime(startedAt), messageSize);
            throw;
        } catch (InboundMailStorageQuotaExceededException) {
            MailInboxTelemetry.RecordIngestion("storage_quota", Stopwatch.GetElapsedTime(startedAt), messageSize);
            return new SmtpResponse(SmtpReplyCode.InsufficientStorage, "Mail storage quota is temporarily exhausted.");
        } catch (Exception) {
            activity?.SetStatus(ActivityStatusCode.Error);
            MailInboxTelemetry.RecordIngestion("failure", Stopwatch.GetElapsedTime(startedAt), messageSize);
            throw;
        } finally {
            _processingSlots.Release();
        }
    }

    public void Dispose() => _processingSlots.Dispose();

    private static async Task<MimeMessage> ParseMessageAsync(byte[] rawBytes, CancellationToken cancellationToken) {
        var stream = new MemoryStream(rawBytes);
        await using (stream.ConfigureAwait(false)) {
            return await MimeMessage.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? Truncate(string? value, int maxCharacters) =>
        value is null || value.Length <= maxCharacters ? value : value[..maxCharacters];
}
