using System.Buffers;
using System.Net;
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
using SmtpServer.Net;
using System.Diagnostics;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class SmtpInboundMessageStore(
    IInboundMailStore store,
    IOptions<MailInboxSmtpOptions> options,
    MailInboxFixedWindowRateLimiter rateLimiter,
    TimeProvider timeProvider,
    ILogger<SmtpInboundMessageStore> logger) : MessageStore, IDisposable {
    private readonly MailInboxSmtpOptions _options = options.Value;
    private readonly HashSet<string> _allowedRecipients = options.Value.AllowedRecipients
        .Select(static address => address.Trim().ToLowerInvariant())
        .ToHashSet(StringComparer.Ordinal);
    private readonly MailInboxNetworkRange[] _trustedRelayNetworks = [.. options.Value.TrustedRelayNetworks
        .Select(MailInboxNetworkRange.Parse)];
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
            MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.Overloaded, Stopwatch.GetElapsedTime(startedAt), messageSize);
            return new SmtpResponse(SmtpReplyCode.Overloaded, "Mail processing capacity is temporarily exhausted.");
        }

        try {
            if (messageSize == 0) {
                MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.EmptyMessage, Stopwatch.GetElapsedTime(startedAt), messageSize);
                return new SmtpResponse(SmtpReplyCode.TransactionFailed, "Message content is required.");
            }

            if (messageSize > _options.MaxMessageSizeBytes) {
                MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.MessageTooLarge, Stopwatch.GetElapsedTime(startedAt), messageSize);
                return SmtpResponse.SizeLimitExceeded;
            }

            IPAddress? sourceAddress = GetSourceAddress(context);
            if (!rateLimiter.TryAcquire(
                    "ip-bytes",
                    sourceAddress is null ? "unknown" : MailInboxNetworkIdentity.GetKey(sourceAddress),
                    _options.MaxRawBytesPerIpPerHour,
                    TimeSpan.FromHours(1),
                    messageSize)) {
                MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.IpByteRateLimited, Stopwatch.GetElapsedTime(startedAt), messageSize);
                return new SmtpResponse(
                    SmtpReplyCode.InsufficientStorage,
                    "Per-source mail byte capacity is temporarily exhausted.");
            }

            byte[] rawBytes = buffer.ToArray();
            MimeMessage message;
            try {
                message = await ParseMessageAsync(
                    rawBytes,
                    _options.MaxMimeParts,
                    _options.MaxMimeDepth,
                    cancellationToken).ConfigureAwait(false);
            } catch (MimeStructureLimitExceededException) {
                MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.MimePartLimit, Stopwatch.GetElapsedTime(startedAt), messageSize);
                return new SmtpResponse(SmtpReplyCode.TransactionFailed, "MIME part limit exceeded.");
            }

            string[] recipients = [.. transaction.To
                .Select(static mailbox => $"{mailbox.User}@{mailbox.Host}")
                .Where(static address => !string.IsNullOrWhiteSpace(address))
                .Select(static address => address.Trim().ToLowerInvariant())];

            if (recipients.Length == 0 ||
                recipients.Length > _options.MaxRecipientsPerMessage ||
                recipients.Any(recipient => !_allowedRecipients.Contains(recipient))) {
                MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.RecipientLimit, Stopwatch.GetElapsedTime(startedAt), messageSize);
                return SmtpResponse.NoValidRecipientsGiven;
            }

            string? messageId = message.MessageId;
            string? fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address;
            string? envelopeFromAddress = GetEnvelopeFromAddress(transaction);
            string? subject = message.Subject;
            if (!MailInboxStoredMessageLimits.IsWithinLimits(
                    messageId,
                    fromAddress,
                    recipients,
                    subject,
                    envelopeFromAddress)) {
                MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.MetadataLimit, Stopwatch.GetElapsedTime(startedAt), messageSize);
                return new SmtpResponse(SmtpReplyCode.TransactionFailed, "Message metadata exceeds the allowed limits.");
            }

            var inboundMessage = InboundMailMessage.Receive(
                messageId,
                fromAddress,
                recipients,
                subject,
                Truncate(message.TextBody, _options.MaxExtractedBodyCharacters),
                Truncate(message.HtmlBody, _options.MaxExtractedBodyCharacters),
                rawBytes,
                timeProvider.GetUtcNow());

            bool isTrustedRelay = sourceAddress is not null &&
                                  _trustedRelayNetworks.Any(range => range.Contains(sourceAddress));
            var admission = new InboundMailAdmission(isTrustedRelay, envelopeFromAddress);
            InboundMailSaveResult saveResult = await store
                .SaveAsync(inboundMessage, admission, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Inbound email accepted. StoredId={StoredId}; RecipientCount={RecipientCount}; Duplicate={Duplicate}",
                saveResult.Id,
                recipients.Length,
                saveResult.WasDuplicate);

            MailInboxTelemetry.RecordIngestion(
                saveResult.WasDuplicate ? MailInboxIngestionOutcome.Duplicate : MailInboxIngestionOutcome.Success,
                Stopwatch.GetElapsedTime(startedAt),
                messageSize);
            return SmtpResponse.Ok;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.Canceled, Stopwatch.GetElapsedTime(startedAt), messageSize);
            throw;
        } catch (InboundMailStorageQuotaExceededException) {
            MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.StorageQuota, Stopwatch.GetElapsedTime(startedAt), messageSize);
            return new SmtpResponse(SmtpReplyCode.InsufficientStorage, "Mail storage quota is temporarily exhausted.");
        } catch (Exception) {
            activity?.SetStatus(ActivityStatusCode.Error);
            MailInboxTelemetry.RecordIngestion(MailInboxIngestionOutcome.Failure, Stopwatch.GetElapsedTime(startedAt), messageSize);
            throw;
        } finally {
            _processingSlots.Release();
        }
    }

    public void Dispose() => _processingSlots.Dispose();

    private static IPAddress? GetSourceAddress(ISessionContext? context) {
        if (context is not null &&
            context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out object? value) &&
            value is IPEndPoint endpoint) {
            return endpoint.Address;
        }

        return null;
    }

    private static string? GetEnvelopeFromAddress(IMessageTransaction transaction) {
        string user = transaction.From.User;
        string host = transaction.From.Host;
        return string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(host)
            ? null
            : $"{user.Trim()}@{host.Trim()}".ToLowerInvariant();
    }

    private static async Task<MimeMessage> ParseMessageAsync(
        byte[] rawBytes,
        int maxMimeParts,
        int maxMimeDepth,
        CancellationToken cancellationToken) {
        var stream = new MemoryStream(rawBytes);
        await using (stream.ConfigureAwait(false)) {
            ParserOptions parserOptions = ParserOptions.Default.Clone();
            parserOptions.MaxMimeDepth = maxMimeDepth;
            var parser = new MimeParser(parserOptions, stream, MimeFormat.Entity);
            int mimePartCount = 0;
            parser.MimeEntityBegin += (_, _) => {
                if (Interlocked.Increment(ref mimePartCount) > maxMimeParts) {
                    throw new MimeStructureLimitExceededException();
                }
            };
            return await parser.ParseMessageAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? Truncate(string? value, int maxCharacters) =>
        value is null || value.Length <= maxCharacters ? value : value[..maxCharacters];

    private sealed class MimeStructureLimitExceededException : Exception;
}
