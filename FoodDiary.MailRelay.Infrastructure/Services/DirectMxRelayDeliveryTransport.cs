using MailKit.Net.Smtp;
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
    ILogger<DirectMxRelayDeliveryTransport> logger) : IRelayDeliveryTransport {
    private static readonly TimeSpan HtmlToTextRegexTimeout = TimeSpan.FromSeconds(1);
    private readonly DirectMxOptions _options = options.Value;

    public async Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        IEnumerable<IGrouping<string, MailboxAddress>> recipientsByDomain = request.To
            .Select(static recipient => MailboxAddress.Parse(recipient))
            .GroupBy(static recipient => recipient.Domain, StringComparer.OrdinalIgnoreCase);

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
        using var client = new SmtpClient();

        if (!string.IsNullOrWhiteSpace(_options.LocalDomain)) {
            client.LocalDomain = _options.LocalDomain;
        }

        Socket socket = await ConnectToAllowedMxEndpointAsync(mxHost, linkedToken.Token).ConfigureAwait(false);
        await client.ConnectAsync(socket, mxHost, _options.Port, secureSocketOptions, linkedToken.Token).ConfigureAwait(false);
        await client.SendAsync(CreateMessage(request, recipients), linkedToken.Token).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, linkedToken.Token).ConfigureAwait(false);
    }

    private async Task<Socket> ConnectToAllowedMxEndpointAsync(string mxHost, CancellationToken cancellationToken) {
        IPAddress[] addresses = IPAddress.TryParse(mxHost, out IPAddress? literalAddress)
            ? [literalAddress]
            : await Dns.GetHostAddressesAsync(mxHost, cancellationToken).ConfigureAwait(false);
        IPAddress? publicAddress = addresses.FirstOrDefault(IsPublicAddress) ?? throw new InvalidOperationException($"Direct MX host '{mxHost}' resolves only to private or loopback addresses.");
        var socket = new Socket(publicAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
            NoDelay = true,
        };

        try {
            await socket.ConnectAsync(new IPEndPoint(publicAddress, _options.Port), cancellationToken).ConfigureAwait(false);
            return socket;
        } catch {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPublicAddress(IPAddress address) {
        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6None)) {
            return false;
        }

        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        switch (address.AddressFamily) {
            case AddressFamily.InterNetwork: {
                    byte[] bytes = address.GetAddressBytes();
                    return bytes[0] != 10 &&
                           bytes[0] != 127 &&
                           !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                           !(bytes[0] == 192 && bytes[1] == 168) &&
                           !(bytes[0] == 169 && bytes[1] == 254) &&
                           !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                           bytes[0] != 0 &&
                           bytes[0] < 224;
                }
            case AddressFamily.InterNetworkV6: {
                    byte[] bytes = address.GetAddressBytes();
                    return !address.IsIPv6LinkLocal &&
                           !address.IsIPv6SiteLocal &&
                           !address.IsIPv6Multicast &&
                           (bytes[0] & 0xfe) != 0xfc;
                }
            default:
                return false;
        }
    }

    private MimeMessage CreateMessage(RelayEmailMessageRequest request, IReadOnlyList<MailboxAddress> recipients) {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.FromName, request.FromAddress));
        message.Subject = request.Subject;
        message.To.AddRange(recipients);
        message.Body = CreateBody(request);
        message.Date = DateTimeOffset.UtcNow;
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
