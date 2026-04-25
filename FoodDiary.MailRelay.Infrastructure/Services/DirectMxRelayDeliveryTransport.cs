using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using System.Net;
using System.Text.RegularExpressions;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DirectMxRelayDeliveryTransport(
    IOptions<DirectMxOptions> options,
    IMxResolver mxResolver,
    DkimSigningService dkimSigningService,
    ILogger<DirectMxRelayDeliveryTransport> logger) : IRelayDeliveryTransport {
    private readonly DirectMxOptions _options = options.Value;

    public async Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        var recipientsByDomain = request.To
            .Select(static recipient => MailboxAddress.Parse(recipient))
            .GroupBy(static recipient => recipient.Domain, StringComparer.OrdinalIgnoreCase);

        foreach (var recipientGroup in recipientsByDomain) {
            await SendToDomainAsync(request, recipientGroup.Key, recipientGroup.ToArray(), cancellationToken);
        }
    }

    private async Task SendToDomainAsync(
        RelayEmailMessageRequest request,
        string domain,
        IReadOnlyList<MailboxAddress> recipients,
        CancellationToken cancellationToken) {
        var mxRecords = await mxResolver.ResolveAsync(domain, cancellationToken);
        Exception? lastError = null;

        foreach (var mxRecord in mxRecords) {
            try {
                await SendToMxAsync(request, recipients, mxRecord.Host, cancellationToken);
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
        var secureSocketOptions = _options.UseStartTlsWhenAvailable
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds));
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using var client = new SmtpClient();

        await client.ConnectAsync(mxHost, _options.Port, secureSocketOptions, linkedToken.Token);
        await client.SendAsync(CreateMessage(request, recipients), linkedToken.Token);
        await client.DisconnectAsync(true, linkedToken.Token);
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
            HtmlBody = request.HtmlBody
        };

        if (!string.IsNullOrWhiteSpace(request.TextBody)) {
            bodyBuilder.TextBody = request.TextBody;
        } else {
            bodyBuilder.TextBody = HtmlToText(request.HtmlBody);
        }

        return bodyBuilder.ToMessageBody();
    }

    private static string HtmlToText(string htmlBody) {
        if (string.IsNullOrWhiteSpace(htmlBody)) {
            return string.Empty;
        }

        var withoutTags = Regex.Replace(htmlBody, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(withoutTags);
    }
}
