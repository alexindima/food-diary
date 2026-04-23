using FoodDiary.MailRelay.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using System.Net;
using System.Text.RegularExpressions;

namespace FoodDiary.MailRelay.Services;

public sealed class SmtpRelayDeliveryTransport(
    IOptions<MailRelaySmtpOptions> options,
    DkimSigningService dkimSigningService) : IRelayDeliveryTransport {
    private readonly MailRelaySmtpOptions _options = options.Value;

    public async Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.FromName, request.FromAddress));
        message.Subject = request.Subject;

        foreach (var recipient in request.To) {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        message.Body = CreateBody(request);
        message.Date = DateTimeOffset.UtcNow;
        message.MessageId = MimeUtils.GenerateMessageId();

        if (dkimSigningService.IsEnabled) {
            dkimSigningService.Sign(message);
        }

        using var client = new SmtpClient();
        var secureSocketOptions = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.User)) {
            await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
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
