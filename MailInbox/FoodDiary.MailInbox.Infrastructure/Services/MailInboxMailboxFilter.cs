using System.Net;
using FoodDiary.MailInbox.Application.Telemetry;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Net;
using SmtpServer.Storage;
using FoodDiary.MailInbox.Infrastructure.Options;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxMailboxFilter(
    IOptions<MailInboxSmtpOptions> options,
    MailInboxSlidingWindowRateLimiter rateLimiter) : MailboxFilter {
    private const string MessageCountKey = "FoodDiary.MailInbox.MessageCount";
    private const string RecipientCountKey = "FoodDiary.MailInbox.RecipientCount";
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);
    private readonly MailInboxSmtpOptions _options = options.Value;
    private readonly HashSet<string> _allowedRecipients = options.Value.AllowedRecipients
        .Select(static value => value.Trim().ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly MailInboxNetworkRange[] _trustedRelayNetworks = [.. options.Value.TrustedRelayNetworks
        .Select(MailInboxNetworkRange.Parse)];

    public override Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox from,
        int size,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (size > _options.MaxMessageSizeBytes) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.MessageTooLarge);
            return Task.FromResult(false);
        }

        IPAddress? remoteAddress = GetRemoteAddress(context);
        if (!_options.AllowUntrustedSources &&
            (remoteAddress is null || !_trustedRelayNetworks.Any(range => range.Contains(remoteAddress)))) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.SourceNotTrusted);
            return Task.FromResult(false);
        }

        if (!TryStartSessionMessage(context)) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.SessionRateLimited);
            return Task.FromResult(false);
        }

        string sourceAddress = remoteAddress is null ? "unknown" : MailInboxNetworkIdentity.GetKey(remoteAddress);
        if (!rateLimiter.TryAcquire(
                "ip",
                sourceAddress,
                _options.MaxMessagesPerIpPerHour,
                RateLimitWindow)) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.IpRateLimited);
            return Task.FromResult(false);
        }

        if (!rateLimiter.TryAcquire(
                "sender",
                string.Concat(sourceAddress, "\n", from.AsAddress()),
                _options.MaxMessagesPerSenderPerHour,
                RateLimitWindow)) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.SenderRateLimited);
            return Task.FromResult(false);
        }

        MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.Accepted);
        return Task.FromResult(true);
    }

    public override Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox from,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        string address = to.AsAddress().Trim().ToLowerInvariant();
        if (!_allowedRecipients.Contains(address)) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.RecipientNotAllowed);
            return Task.FromResult(false);
        }

        if (!TryAddRecipient(context)) {
            MailInboxTelemetry.RecordAdmission(MailInboxAdmissionOutcome.RecipientLimitExceeded);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private bool TryStartSessionMessage(ISessionContext? context) {
        if (context is null) {
            return true;
        }

        lock (context.Properties) {
            int messageCount = GetCount(context, MessageCountKey);
            if (messageCount >= _options.MaxMessagesPerSession) {
                return false;
            }

            context.Properties[MessageCountKey] = messageCount + 1;
            context.Properties[RecipientCountKey] = 0;
            return true;
        }
    }

    private bool TryAddRecipient(ISessionContext? context) {
        if (context is null) {
            return true;
        }

        lock (context.Properties) {
            int recipientCount = GetCount(context, RecipientCountKey);
            if (recipientCount >= _options.MaxRecipientsPerMessage) {
                return false;
            }

            context.Properties[RecipientCountKey] = recipientCount + 1;
            return true;
        }
    }

    private static int GetCount(ISessionContext context, string key) =>
        context.Properties.TryGetValue(key, out object? value) && value is int count ? count : 0;

    private static IPAddress? GetRemoteAddress(ISessionContext? context) {
        if (context is not null &&
            context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out object? value) &&
            value is IPEndPoint endpoint) {
            return endpoint.Address;
        }

        return null;
    }
}
