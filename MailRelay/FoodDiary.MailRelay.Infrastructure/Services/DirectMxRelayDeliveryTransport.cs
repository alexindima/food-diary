using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DirectMxRelayDeliveryTransport(
    IOptions<DirectMxOptions> options,
    IMxResolver mxResolver,
    DkimSigningService dkimSigningService,
    TimeProvider timeProvider,
    ILogger<DirectMxRelayDeliveryTransport> logger,
    IDirectMxEndpointConnector? endpointConnector = null,
    IDirectMxSmtpClientFactory? smtpClientFactory = null) : IRelayDeliveryTransport {
    private static readonly TimeSpan HtmlToTextRegexTimeout = TimeSpan.FromSeconds(1);
    private readonly DirectMxOptions _options = options.Value;
    private readonly IDirectMxEndpointConnector _endpointConnector = endpointConnector ?? new DirectMxEndpointConnector();
    private readonly IDirectMxSmtpClientFactory _smtpClientFactory = smtpClientFactory ?? new DirectMxSmtpClientFactory();

    public async Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        IGrouping<string, MailboxAddress>[] recipientsByDomain = [.. request.To
            .Select(static recipient => MailboxAddress.Parse(recipient))
            .GroupBy(static recipient => recipient.Domain, StringComparer.OrdinalIgnoreCase)];

        if (recipientsByDomain.Length > 1) {
            throw new InvalidOperationException("Direct MX delivery supports recipients from one domain per queued message to avoid partial cross-domain delivery.");
        }

        foreach (IGrouping<string, MailboxAddress> recipientGroup in recipientsByDomain) {
            await SendToDomainAsync(request, recipientGroup.Key, recipientGroup.ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendToDomainAsync(
        RelayEmailMessageRequest request,
        string domain,
        IReadOnlyList<MailboxAddress> recipients,
        CancellationToken cancellationToken) {
        IReadOnlyList<MxRecord> mxRecords = await mxResolver.ResolveAsync(domain, cancellationToken).ConfigureAwait(false);
        Exception? lastError = null;

        foreach (MxRecord mxRecord in mxRecords) {
            try {
                await SendToMxAsync(request, recipients, mxRecord.Host, cancellationToken).ConfigureAwait(false);
                return;
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                lastError = ex;
                logger.LogWarning(
                    ex,
                    "Direct MX delivery to {Domain} via {MxHost} failed.",
                    domain,
                    mxRecord.Host);
            }
        }

        throw new InvalidOperationException($"Direct MX delivery to '{domain}' failed for every MX host.", lastError);
    }

    private async Task SendToMxAsync(
        RelayEmailMessageRequest request,
        IReadOnlyList<MailboxAddress> recipients,
        string mxHost,
        CancellationToken cancellationToken) {
        SecureSocketOptions secureSocketOptions = _options.UseStartTlsWhenAvailable
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds));
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using IDirectMxSmtpClient client = _smtpClientFactory.Create();

        if (!string.IsNullOrWhiteSpace(_options.LocalDomain)) {
            client.LocalDomain = _options.LocalDomain;
        }

        Socket socket = await _endpointConnector.ConnectAsync(mxHost, _options.Port, linkedToken.Token).ConfigureAwait(false);
        await client.ConnectAsync(socket, mxHost, _options.Port, secureSocketOptions, linkedToken.Token).ConfigureAwait(false);
        await client.SendAsync(CreateMessage(request, recipients), linkedToken.Token).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, linkedToken.Token).ConfigureAwait(false);
    }

    private MimeMessage CreateMessage(RelayEmailMessageRequest request, IReadOnlyList<MailboxAddress> recipients) {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.FromName, request.FromAddress));
        message.Subject = request.Subject;
        message.To.AddRange(recipients);
        message.Body = CreateBody(request);
        message.Date = timeProvider.GetUtcNow();
        message.MessageId = MimeUtils.GenerateMessageId();

        if (dkimSigningService.IsEnabled) {
            dkimSigningService.Sign(message);
        }

        return message;
    }

    private static MimeEntity CreateBody(RelayEmailMessageRequest request) {
        var bodyBuilder = new BodyBuilder {
            HtmlBody = request.HtmlBody,
            TextBody = !string.IsNullOrWhiteSpace(request.TextBody) ? request.TextBody : HtmlToText(request.HtmlBody),
        };

        return bodyBuilder.ToMessageBody();
    }

    private static string HtmlToText(string htmlBody) {
        if (string.IsNullOrWhiteSpace(htmlBody)) {
            return string.Empty;
        }

        string withoutTags = Regex.Replace(htmlBody, "<[^>]+>", " ", RegexOptions.None, HtmlToTextRegexTimeout);
        return WebUtility.HtmlDecode(withoutTags);
    }
}
